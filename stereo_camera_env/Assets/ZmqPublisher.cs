using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;

public sealed class ZmqPublisher : IDisposable
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

    public void Dispose()
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