using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class AsyncZmqCamera : MonoBehaviour
{
    public string address = "tcp://*:5556";
    public string topic = "camera";
    public int fps = 30;

    (RenderTexture grab, RenderTexture flip) _rt;
    NativeArray<byte>[] _buffers;
    private Camera _camera;
    private ZmqPublisher _publisher;

    IEnumerator Start()
    {
        _publisher = new ZmqPublisher(address, topic);

        _camera = GetComponent<Camera>();
        var (w, h) = (1920, 1080);

        _rt.grab = new RenderTexture(w, h, 0);
        _rt.flip = new RenderTexture(w, h, 0);

        _camera.targetTexture = _rt.grab;

        _buffers = new NativeArray<byte>[fps];
        for (int i = 0; i < fps; i++)
        {
            _buffers[i] = new NativeArray<byte>(w * h * 4, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
        }

        float frameTime = 1f / fps;
        int frameCount = 0;
        while (true)
        {
            yield return new WaitForSeconds(frameTime);
            yield return new WaitForEndOfFrame();

            Graphics.Blit(_rt.grab, _rt.flip);

            AsyncGPUReadback.RequestIntoNativeArray
                (ref _buffers[frameCount % fps], _rt.flip, 0, OnCompleteReadback);

            frameCount++;
        }
    }

    void OnDestroy()
    {
        _publisher.Destroy();

        AsyncGPUReadback.WaitAllRequests();

        Destroy(_rt.flip);
        Destroy(_rt.grab);

        foreach (NativeArray<byte> buffer in _buffers)
        {
            buffer.Dispose();
        }
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogException(new Exception("GPU Readback Error."));
            return;
        }

        _publisher.SendBytes(request.GetData<byte>().ToArray());
    }
}