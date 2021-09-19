using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using TMPro.EditorUtilities;
using UnityEngine;

public class TcpCamera : MonoBehaviour
{
    public string address = "127.0.0.1";
    public ushort port = 5005;
    private TcpSocket _tcpSocket;
    private byte[] _header;

    private Camera _camera;
    private Texture2D _texture;
    private Rect _frame;
    private Color[] _colors;

    void Start()
    {
        _camera = GetComponent<Camera>();

        RenderTexture targetTexture = _camera.targetTexture;
        if (targetTexture == null)
        {
            Debug.LogException(new ArgumentException("Camera of UdpCamera requires a target texture"));
        }

        int w = targetTexture.width;
        int h = targetTexture.height;

        _texture = new Texture2D(w, h, TextureFormat.RGB24, false);
        _frame = new Rect(0, 0, w, h);
        _colors = new Color[w * h];

        _tcpSocket = new TcpSocket(address, port);

        byte[] wTag = Encoding.ASCII.GetBytes("w=");
        byte[] hTag = Encoding.ASCII.GetBytes("h=");
        byte[] wBytes = BitConverter.GetBytes(w);
        Array.Reverse(wBytes);
        byte[] hBytes = BitConverter.GetBytes(h);
        Array.Reverse(hBytes);
        
        _header = new byte[wTag.Length + wBytes.Length + hTag.Length + hBytes.Length];
        wTag.CopyTo(_header, 0);
        wBytes.CopyTo(_header, wTag.Length);
        hTag.CopyTo(_header, wTag.Length + wBytes.Length);
        hBytes.CopyTo(_header, wTag.Length + wBytes.Length + hTag.Length);
    }

    public void OnPostRender()
    {
        RenderTexture prevTexture = RenderTexture.active;
        RenderTexture.active = _camera.activeTexture;

        _texture.ReadPixels(_frame, 0, 0);
        _texture.GetPixels().CopyTo(_colors, 0);

        RenderTexture.active = prevTexture;

        byte[] r = _colors.Select(x => (byte)(x.r * 255)).ToArray();
        byte[] g = _colors.Select(x => (byte)(x.g * 255)).ToArray();
        byte[] b = _colors.Select(x => (byte)(x.b * 255)).ToArray();
        byte[] pixels = new byte[_colors.Length * 3];
        r.CopyTo(pixels, 0);
        g.CopyTo(pixels, r.Length);
        b.CopyTo(pixels, r.Length + g.Length);
        
        _tcpSocket.SendBytes(_header);
        
        // byte[] test = new byte[32768 + 10];
        // test[0] = 255;
        // test[1] = 128;
        // test[32768] = 254;
        // test[32769] = 127;
        // _tcpSocket.SendBytes(test);
        _tcpSocket.SendBytes(pixels);
        
        Debug.Log("");
    }
}

public class TcpSocket
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _networkStream;

    public TcpSocket(string server, ushort port)
    {
        _tcpClient = new TcpClient(server, port);
        _networkStream = _tcpClient.GetStream();
    }

    ~TcpSocket()
    {
        _networkStream.Close();
        _tcpClient.Close();
    }

    public void SendBytes(byte[] bytes)
    {
        _networkStream.Write(bytes, 0, bytes.Length);
    }
}