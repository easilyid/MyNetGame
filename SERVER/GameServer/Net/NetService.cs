﻿using System.Net;
using Common;
using Proto;
using Serilog;

namespace GameServer;

/// <summary>
/// 网络服务
/// </summary>
public class NetService : Singleton<NetService>
{
    //负责监听TCP连接
    TcpServer tcpServer;

    //记录conn最后一次心跳包的时间
    private Dictionary<Connection, DateTime> heartBeatPairs = new Dictionary<Connection, DateTime>();

    //心跳超时时间
    private static int HEARTBEATTIMEOUT = 5;

    //服务器查询心跳字典的间隔时间
    private static int HEARTBEATQUERYTIME = 5;

    /// <summary>
    /// 开启当前服务
    /// </summary>
    public void Init()
    {
        //启动网络监听
        tcpServer = new TcpServer();
        tcpServer.Init(Config.Server.ip, Config.Server.serverPort, OnConnected, OnDisconnected);

        //启动消息分发器
        MessageRouter.Instance.Start(Config.Server.workerCount);

        //订阅心跳事件
        MessageRouter.Instance.Subscribe<HeartBeatRequest>(_HeartBeatRequest);

        //定时检查心跳包的情况
        Timer timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(HEARTBEATQUERYTIME));
    }

    /// <summary>
    /// 客户端连接成功的回调
    /// </summary>
    /// <param name="conn"></param>
    private void OnConnected(Connection conn)
    {
        try
        {
            if (conn.Socket != null && conn.Socket.Connected)
            {
                var ipe = conn.Socket.RemoteEndPoint;
                Log.Information("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " +
                                ((IPEndPoint)ipe).Port.ToString());

                // 给conn添加心跳时间
                heartBeatPairs[conn] = DateTime.Now;
            }
            else
            {
                Log.Warning("[NetService]尝试访问已关闭的 Socket 对象");
            }
        }
        catch (ObjectDisposedException ex)
        {
            Log.Error("[NetService]Socket 已被释放: " + ex.Message);
        }
    }

    /// <summary>
    /// 客户端断开连接回调
    /// </summary>
    /// <param name="conn"></param>
    private void OnDisconnected(Connection conn)
    {
        //从心跳字典中删除连接
        if (heartBeatPairs.ContainsKey(conn))
        {
            heartBeatPairs.Remove(conn);
        }

        //session  从链接里获取到 网络会话
        var session = conn.Get<Session>();
        if (session != null)
        {
            session.Conn = null;
        }

        //测试信息
        if (session != null)
        {
            //TODO： 这里可以考虑将session的连接关闭
            //Log.Information("[连接断开]用户名：" + session.dbUser.Username);
        }
        else
        {
            Log.Information("[连接断开]未知用户");
        }
    }

    /// <summary>
    /// 接收到心跳包的处理
    /// </summary>
    /// <param name="conn"></param>
    /// <param name="message"></param>
    public void _HeartBeatRequest(Connection conn, HeartBeatRequest message)
    {
        //更新心跳时间
        heartBeatPairs[conn] = DateTime.Now;
        var session = conn.Get<Session>();
        if (session != null)
        {
            session.LastHeartTime = MyTime.time;
        }

        //响应
        HeartBeatResponse resp = new HeartBeatResponse();
        conn.Send(resp);
    }

    /// <summary>
    /// 检查心跳包的回调,这里是自己启动了一个timer。可以考虑交给中心计时器
    /// </summary>
    /// <param name="state"></param>
    private void TimerCallback(object state)
    {
        DateTime nowTime = DateTime.Now;
        //这里规定心跳包超过30秒没用更新就将连接清理
        foreach (var kv in heartBeatPairs)
        {
            TimeSpan gap = nowTime - kv.Value;
            if (gap.TotalSeconds > HEARTBEATTIMEOUT)
            {
                //关闭超时的客户端连接
                Connection conn = kv.Key;
                Log.Information("[心跳检查] 客户端心跳超时！"); //移除相关的资源
                ActiveClose(conn);
            }
        }
    }

    /// <summary>
    /// 主动关闭某个连接
    /// </summary>
    public void ActiveClose(Connection conn)
    {
        if (conn == null) return;

        //从心跳字典中删除连接
        if (heartBeatPairs.ContainsKey(conn))
        {
            heartBeatPairs.Remove(conn);
        }

        //session
        var session = conn.Get<Session>();
        if (session != null)
        {
            session.Conn = null;
        }

        //转交给下一层的connection去进行关闭
        conn.ActiveClose();
    }
}