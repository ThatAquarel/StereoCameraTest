using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ZmqCamera : MonoBehaviour
{
    public string address = "tcp://*:5556";
    public string topic = "camera";
    public int width = 1920;
    public int height = 1080;

    private ZmqPublisher _publisher;

    private Camera _camera;
    private Texture2D _texture;
    private Rect _frame;

    void Start()
    {
        _camera = GetComponent<Camera>();

        RenderTexture targetTexture = new RenderTexture(width, height, 24);
        _camera.targetTexture = targetTexture;

        _texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        _frame = new Rect(0, 0, width, height);

        _publisher = new ZmqPublisher(address, topic);
    }

    private void OnDestroy()
    {
        _publisher.Destroy();
    }

    private void OnPostRender()
    {
        RenderTexture prevTexture = RenderTexture.active;
        RenderTexture.active = _camera.activeTexture;

        _texture.ReadPixels(_frame, 0, 0);
        _publisher.SendBytes(_texture.GetRawTextureData());

        RenderTexture.active = prevTexture;
    }
}

class ZmqPublisher
{
    private readonly PublisherSocket _publisherSocket;
    private readonly string _topic;

    public ZmqPublisher(string address, string topic)
    {
        _topic = topic;

        ForceDotNet.Force();
        NetMQConfig.Cleanup();
        _publisherSocket = new PublisherSocket();
        _publisherSocket.Bind(address);
    }

    public void Destroy()
    {
        _publisherSocket.Close();
        _publisherSocket.Dispose();

        NetMQConfig.Cleanup();
    }

    public void SendBytes(byte[] bytes)
    {
        _publisherSocket
            .SendMoreFrame(_topic)
            .SendFrame(bytes);
    }
}