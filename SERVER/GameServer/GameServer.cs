using Common;
using Serilog;

namespace GameServer;

public class GameServer
{
    public static void Init()
    {
        SerilogManager.Instance.Init();
        Config.Init();

        //启动计时器 用于更新
        //Scheduler.Instance.Start(Config.Server.updateHz);


        Log.Information("[GameServer]初始化,配置如下：");
        Log.Information("Ip：{0}", Config.Server.ip);
        Log.Information("ServerPort：{0}", Config.Server.serverPort);
        Log.Information("WorkerCount：{0}", Config.Server.workerCount);
        Log.Information("UpdateHz：{0}", Config.Server.updateHz);
        Log.Information("AoiViewArea：{0}", Config.Server.aoiViewArea);
        Log.Information("HeartBeatTimeOut：{0}", Config.Server.heartBeatTimeOut);
        Log.Information("\x1b[32m" + "=============================================" + "\x1b[0m");

        Log.Debug("[Proto协议加载]");
        ProtoHelper.Init();

        //开启网络服务
        Log.Debug("[启动网络服务]");
        NetService.Instance.Init();
    }
}