using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class AsyncZmqCamera : MonoBehaviour
{
    public string address = "tcp://*:5556";
    public string topic = "camera";
    public int frameWidth = 1920;
    public int frameHeight = 1080;
    public int fps = 30;

    private (RenderTexture grab, RenderTexture flip) _rt;
    private NativeArray<byte>[] _buffers;
    private Camera _camera;
    private ZmqPublisher _publisher;
    private bool _isRunning = true;

    IEnumerator Start()
    {
        _publisher = new ZmqPublisher(address, topic);

        _rt.grab = new RenderTexture(frameWidth, frameHeight, 0);
        _rt.flip = new RenderTexture(frameWidth, frameHeight, 0);
        _camera = GetComponent<Camera>();
        _camera.targetTexture = _rt.grab;

        _buffers = new NativeArray<byte>[fps];
        for (int i = 0; i < fps; i++)
        {
            _buffers[i] = new NativeArray<byte>(frameWidth * frameHeight * 4,
                Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        float frameTime = 1f / fps;
        int frameCount = 0;
        while (_isRunning)
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
        _isRunning = false;

        _publisher.Dispose();

        AsyncGPUReadback.WaitAllRequests();
        Destroy(_rt.flip);
        Destroy(_rt.grab);

        foreach (NativeArray<byte> buffer in _buffers) buffer.Dispose();
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