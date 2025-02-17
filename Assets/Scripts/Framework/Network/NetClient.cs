﻿using System;
using System.IO;
using System.Net;
using UnityEngine;
using System.Net.Sockets;

public class NetClient
{
    TcpClient Client;
    NetworkStream Tcpstream; //网络流
    MemoryStream MemStream;  //内存流
    BinaryReader BinaryReader;
    const int BufferSize = 1024 * 64; //缓存大小
    byte[] Buffer = new byte[BufferSize]; //缓冲区

    public NetClient()
    {
        MemStream = new MemoryStream();
        BinaryReader = new BinaryReader(MemStream);
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="host">地址</param>
    /// <param name="port">端口</param>
    public void OnConnectServer(string host, int port)
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
            {
                Debug.LogError("Host invalid");
                return;
            }
            if (addresses[0].AddressFamily == AddressFamily.InterNetworkV6)
            {
                Client = new TcpClient(AddressFamily.InterNetworkV6);
            }
            else
            {
                Client = new TcpClient(AddressFamily.InterNetwork);
            }
            Client.SendTimeout = 1000;
            Client.ReceiveTimeout = 1000;
            Client.NoDelay = true;
            Client.BeginConnect(host, port, OnConnect, null);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    void OnDisConnected()
    {
        if (Client != null && Client.Connected)
        {
            Client.Close();
            Client = null;
            Tcpstream.Close();
            Tcpstream = null;
        }
        GameManager.Instance.GetManager<NetManager>(GameManager.ManagerName.Net).DisConnected();
    }

    /// <summary>
    /// 连接服务器回调
    /// </summary>
    /// <param name="asyncResult"></param>
    void OnConnect(IAsyncResult asyncResult)
    {
        if (Client == null || !Client.Connected)
        {
            Debug.LogError("Connect Server Error!");
            return;
        }
        GameManager.Instance.GetManager<NetManager>(GameManager.ManagerName.Net).NetConnected();
        Tcpstream = Client.GetStream();
        Tcpstream.BeginRead(Buffer, 0, BufferSize, OnRead, null);
    }

    /// <summary>
    /// 读取
    /// </summary>
    /// <param name="asyncResult"></param>
    void OnRead(IAsyncResult asyncResult)
    {
        try
        {
            if (Client == null || Tcpstream == null)
            {
                return;
            }
            if (Buffer.Length < 1)
            {
                OnDisConnected();
                return;
            }
            ReceiveData();
            lock (Tcpstream)
            {
                Array.Clear(Buffer, 0, Buffer.Length);
                Tcpstream.BeginRead(Buffer, 0, BufferSize, OnRead, null);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            OnDisConnected();
        }
    }

    /// <summary>
    /// 解析数据
    /// </summary>
    void ReceiveData()
    {
        MemStream.Seek(0, SeekOrigin.End);  //设置内存时钟为末尾
        MemStream.Write(Buffer,0,Buffer.Length); //收到的数据
        MemStream.Seek(0, SeekOrigin.Begin); //指针移动到头部
        while (RemainingBytesLength() >= 8)
        {
            int msgid = BinaryReader.ReadInt32();
            int msgLen = BinaryReader.ReadInt32();
            if (RemainingBytesLength() >= msgLen)
            {
                byte[] data = BinaryReader.ReadBytes(msgLen);
                string message = System.Text.Encoding.UTF8.GetString(data);
                GameManager.Instance.GetManager<NetManager>(GameManager.ManagerName.Net).Receive(msgid, message); //转到Lua
            }
            else
            {
                MemStream.Position = MemStream.Position - 8;
                break;
            }
        }
        byte[] leftover = BinaryReader.ReadBytes(RemainingBytesLength()); //剩余下字节重新归还
        MemStream.SetLength(0);
        MemStream.Write(leftover,0,leftover.Length);
    }

    /// <summary>
    /// 剩余字节数
    /// </summary>
    int RemainingBytesLength() => (int) (MemStream.Length - MemStream.Position);

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="msgid">发送ID</param>
    /// <param name="message">消息体</param>
    public void SendMessage(int msgid, string message)
    {
        using (MemoryStream ms=new MemoryStream())
        {
            ms.Position = 0;
            BinaryWriter bw = new BinaryWriter(ms);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
            bw.Write(msgid); //消息ID
            bw.Write((int)data.Length); //消息长度
            bw.Write(data); //消息内容
            bw.Flush();
            if (Client != null && Client.Connected)
            {
                byte[] sendData = ms.ToArray();
                Tcpstream.BeginWrite(sendData, 0, sendData.Length, OnEndSend, null);
            }
            else
            {
                Debug.LogError("Server Not Connected");
            }
        }
    }

    /// <summary>
    /// 发送消息回调
    /// </summary>
    /// <param name="arResult"></param>
    void OnEndSend(IAsyncResult  arResult)
    {
        try
        {
            Tcpstream.EndWrite(arResult); //结束发送
        }
        catch (Exception e)
        {
            OnDisConnected();
            Debug.LogError(e.Message);
        }
    }
}

