﻿using System.Net.Sockets;
using Google.Protobuf;
using Serilog;

namespace Common;

public class Connection : TypeAttributeStore
{
    public delegate void DataReceivedHandler(Connection sender, IMessage data);

    public delegate void DisconnectedHandler(Connection sender);

    private Socket m_socket; // 连接客户端的socket
    private LengthFieldDecoder m_lfd; // 消息接受器
    public DataReceivedHandler MOnDataReceived; //消息接收的委托  todo这玩意貌似没有用上，因为消息我们直接交给消息路由了
    public DisconnectedHandler MOnDisconnected; // 关闭连接的委托

    public Socket Socket
    {
        get { return m_socket; }
    }

    public Connection(Socket socket)
    {
        m_socket = socket;

        m_lfd = new LengthFieldDecoder(socket, 64 * 1024, 0, 4, 0, 4);
        m_lfd.MOnDataRecived += OnDataRecived;
        m_lfd.MOnDisconnected += _OnDisconnected;
        m_lfd.Init();
    }

    private void OnDataRecived(byte[] data)

    {
        ushort code = GetUShort(data, 0);
        var msg = ProtoHelper.ParseFrom((int)code, data, 2, data.Length - 2);

        //交给消息路由，让其帮忙转发
        if (MessageRouter.Instance.Running)
        {
            MessageRouter.Instance.AddMessage(this, msg);
        }
    }

    /// <summary>
    /// 获取data数据，偏移offset。获取两个字节
    /// 前提：data必须是大端字节序
    /// </summary>
    /// <param name="data"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    private ushort GetUShort(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian)
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        return (ushort)((data[offset + 1] << 8) | data[offset]);
    }

    #region 发送网络数据包

    /// <summary>
    /// 发送消息包，编码过程(通用)
    /// </summary>
    /// <param name="message"></param>
    public void Send(Google.Protobuf.IMessage message)
    {
        try
        {
            //获取imessage类型所对应的编号，网络传输我们只传输编号
            using (var ds = DataStream.Allocate())
            {
                int code = ProtoHelper.SeqCode(message.GetType());
                ds.WriteInt(message.CalculateSize() + 2); //长度字段
                ds.WriteUShort((ushort)code); //协议编号字段
                message.WriteTo(ds); //数据
                SocketSend(ds.ToArray());
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    /// <summary>
    /// 通过socket发送，原生数据
    /// </summary>
    /// <param name="data"></param>
    private void SocketSend(byte[] data)
    {
        SocketSend(data, 0, data.Length);
    }

    /// <summary>
    /// 开始异步发送消息,原生数据
    /// </summary>
    /// <param name="data"></param>
    /// <param name="start"></param>
    /// <param name="len"></param>
    private void SocketSend(byte[] data, int start, int len)
    {
        lock (this) //多线程问题，防止争夺send
        {
            if (m_socket != null && m_socket.Connected)
            {
                m_socket.BeginSend(data, start, len, SocketFlags.None, new AsyncCallback(SendCallback), m_socket);
            }
        }
    }

    /// <summary>
    /// 异步发送消息回调
    /// </summary>
    /// <param name="ar"></param>
    private void SendCallback(IAsyncResult ar)
    {
        if (m_socket != null && m_socket.Connected)
        {
            // 发送的字节数
            int len = m_socket.EndSend(ar);
        }
    }

    #endregion


    /// <summary>
    /// 断开连接回调
    /// </summary>
    private void _OnDisconnected()
    {
        m_socket = null;
        //向上转发，让其删除本connection对象
        MOnDisconnected?.Invoke(this);
    }

    //todo,我们调用底层解码器的关闭连接函数
    /// <summary>
    /// 主动关闭连接
    /// </summary>
    public void ActiveClose()
    {
        m_socket = null;
        //转交给下一层的解码器关闭连接
        m_lfd.ActiveDisconnection();
    }
}