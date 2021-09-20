// A lot of this code is copied from here:
// https://github.com/keijiro/AsyncCaptureTest/blob/master/Assets/AsyncCapture.cs

using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
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

    private bool _isRunning = true;

    IEnumerator Start()
    {
        ZmqPublisher.Instance.Bind(address);

        _rt.grab = new RenderTexture(frameWidth, frameHeight, 0);
        _rt.flip = new RenderTexture(frameWidth, frameHeight, 0);
        _camera = GetComponent<Camera>();
        _camera.targetTexture = _rt.grab;

        // Use one buffer per frame to give the GPU enough time to write (~1 second)
        // Or we will run into the problem where the buffer cannot be read:
        // https://forum.unity.com/threads/asyncgpureadback-requestintonativearray-
        // causes-invalidoperationexception-on-nativearray.1011955/

        _buffers = new NativeArray<byte>[fps];
        int bufferSize = frameWidth * frameHeight * 4;
        for (int i = 0; i < _buffers.Length; i++)
        {
            _buffers[i] = new NativeArray<byte>(bufferSize, Allocator.Persistent, 
                NativeArrayOptions.UninitializedMemory);
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
        AsyncGPUReadback.WaitAllRequests();
        Destroy(_rt.flip);
        Destroy(_rt.grab);
        foreach (NativeArray<byte> buffer in _buffers) buffer.Dispose();
        ZmqPublisher.Instance.Dispose();

        _isRunning = false;
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogException(new Exception("GPU Readback Error."));
            return;
        }

        ZmqPublisher.Instance.SendBytes(topic, request.GetData<byte>().ToArray());
    }
}