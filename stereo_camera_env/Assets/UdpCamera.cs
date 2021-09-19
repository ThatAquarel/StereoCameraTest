using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UdpCamera : MonoBehaviour
{
    public string address = "127.0.0.1";
    public ushort port = 5005;

    private int _frameWidth;
    private int _frameHeight;

    private Camera _camera;
    private Texture2D _texture;
    private Rect _frame;
    private Color[] _colors;

    private UdpSocket _udpSocket;

    private readonly byte[] _widthTag = Encoding.ASCII.GetBytes("w=");
    private readonly byte[] _heightTag = Encoding.ASCII.GetBytes("h=");
    private byte[] _packetHeader;

    void Start()
    {
        _camera = GetComponent<Camera>();

        var targetTexture = _camera.targetTexture;
        if (targetTexture == null)
        {
            Debug.LogException(new ArgumentException("Camera of UdpCamera requires a target texture"));
        }

        _frameWidth = targetTexture.width;
        _frameHeight = targetTexture.height;

        _texture = new Texture2D(_frameWidth, _frameHeight, TextureFormat.RGB24, false);
        _frame = new Rect(0, 0, _frameWidth, _frameHeight);
        _colors = new Color[_frameWidth * _frameHeight];

        _udpSocket = new UdpSocket(address, port);

        byte[] widthBytes = BitConverter.GetBytes(_frameWidth);
        Array.Reverse(widthBytes);
        byte[] heightBytes = BitConverter.GetBytes(_frameHeight);
        Array.Reverse(heightBytes);

        _packetHeader = new byte[8 + _widthTag.Length + _heightTag.Length];
        _widthTag.CopyTo(_packetHeader, 0);
        widthBytes.CopyTo(_packetHeader, 2);
        _heightTag.CopyTo(_packetHeader, 6);
        heightBytes.CopyTo(_packetHeader, 8);
    }

    public void OnPostRender()
    {
        RenderTexture prevTexture = RenderTexture.active;
        RenderTexture.active = _camera.activeTexture;

        _texture.ReadPixels(_frame, 0, 0);
        _texture.GetPixels().CopyTo(_colors, 0);

        RenderTexture.active = prevTexture;

        int[] r = _colors.Select(x => (int)(x.r * 255)).ToArray();
        int[] g = _colors.Select(x => (int)(x.g * 255)).ToArray();
        int[] b = _colors.Select(x => (int)(x.b * 255)).ToArray();

        byte[] pixels = new byte[_colors.Length * 3];

        for (int i = 0; i < _colors.Length; i++)
        {
            int j = i * 3;
            pixels[j] = (byte)r[i];
            pixels[j + 1] = (byte)g[i];
            pixels[j + 2] = (byte)b[i];
        }

        // byte[] buffer = new byte[_packetHeader.Length + pixels.Length];
        // _packetHeader.CopyTo(buffer, 0);
        // pixels.CopyTo(buffer, _packetHeader.Length);
        // _udpSocket.SendBytes(buffer);

        _udpSocket.SendBytes(_packetHeader);
        _udpSocket.SendBytes(pixels);
    }
}

public class UdpSocket
{
    private readonly Socket _socket;
    private readonly IPEndPoint _endPoint;

    // private int _bufferSize = 1024;
    private int _bufferSize = 16384;

    public UdpSocket(string address, ushort port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _endPoint = new IPEndPoint(IPAddress.Parse(address), port);
    }

    public void SendBytes(byte[] bytes)
    {
        // string text = "Hello";
        // byte[] sendBuffer = Encoding.ASCII.GetBytes(text);
        // _socket.SendTo(sendBuffer, _endPoint);

        for (int i = 0; i < bytes.Length; i = i + _bufferSize)
        {
            int quotient = i / _bufferSize;
            int skip = quotient * _bufferSize;
            int take = Math.Min(bytes.Length - skip, _bufferSize);

            IEnumerable<byte> slicedBytes = bytes.Skip(skip).Take(take);
            _socket.SendTo(slicedBytes.ToArray(), _endPoint);
        }
    }
}