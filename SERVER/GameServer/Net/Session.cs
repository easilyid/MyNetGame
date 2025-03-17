using System.Collections.Concurrent;
using Common;
using Google.Protobuf;
using Serilog;

namespace GameServer;

/// <summary>
/// 用户会话,代表玩家客户端，只有登录成功的用户才有session
/// </summary>
public class Session
{
    public string Id { get; private set; }
    public Connection Conn; //连接对象

    private ConcurrentQueue<IMessage> msgBuffer = new ConcurrentQueue<IMessage>(); //消息缓冲区  如果连接断开 消息就缓冲到这个位置

    public float LastHeartTime { get; set; } // 最后心跳时间

    private Session()
    {
    }

    public Session(string sessionId)
    {
        Id = sessionId;
        LastHeartTime = MyTime.time;
    }

    /// <summary>
    /// 向客户端发送消息
    /// </summary>
    /// <param name="message"></param>
    public void Send(IMessage message)
    {
        if (Conn != null)
        {
            while (msgBuffer.TryDequeue(out var msg))
            {
                //如果有缓存的消息，说明当前连接已经断开了
                Log.Information("补发消息：" + msg);
                Conn.Send(msg);
            }

            Conn.Send(message);
        }
        else
        {
            //说明当前角色离线了，我们将数据写入缓存中
            msgBuffer.Enqueue(message);
        }
    }

    // /// <summary>
    // /// session的离开游戏
    // /// </summary>
    // public void Leave()
    // {
    //     Log.Information("session过期");
    //     //让session失效
    //     SessionManager.Instance.RemoveSession(Id);
    //     //移除chr
    //     if (character != null)
    //     {
    //         character.currentSpace?.EntityLeave(character);
    //         CharacterManager.Instance.RemoveCharacter(character.EntityId);
    //     }
    // }
}