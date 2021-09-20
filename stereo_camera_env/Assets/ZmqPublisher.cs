using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public sealed class ZmqPublisher : IDisposable
{
    public static ZmqPublisher Instance { get; } = new ZmqPublisher();

    private readonly PublisherSocket _publisherSocket;
    private string _address;

    private ZmqPublisher()
    {
        ForceDotNet.Force();
        NetMQConfig.Cleanup();
        _publisherSocket = new PublisherSocket();
    }

    public void Dispose()
    {
        _publisherSocket.Close();
        _publisherSocket.Dispose();

        NetMQConfig.Cleanup();
    }

    public void Bind(string address)
    {
        if (_address == null)
        {
            _publisherSocket.Bind(address);
            _address = address;
        }
        else if (address != _address)
        {
            Debug.LogException(
                new ArgumentException(
                    "Cannot bind to another address"));
        }
    }

    public void SendBytes(string topic, byte[] bytes)
    {
        _publisherSocket
            .SendMoreFrame(topic)
            .SendFrame(bytes);
    }
}