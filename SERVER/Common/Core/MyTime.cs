namespace Common;

//时间单位
public enum TimeUnit
{
    /// <summary>
    /// 毫秒
    /// </summary>
    Milliseconds,

    /// <summary>
    /// 秒
    /// </summary>
    Seconds,

    /// <summary>
    /// 分钟
    /// </summary>
    Minutes,

    /// <summary>
    /// 小时
    /// </summary>
    Hours,

    /// <summary>
    /// 天
    /// </summary>
    Days
}

public class MyTime
{
    private static long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    //游戏的运行时间（秒），帧开始的时间
    public static float time { get; private set; }

    //上一帧运行所用的时间
    public static float deltaTime { get; private set; }

    // 记录最后一次tick的时间
    private static long lastTick = 0;

    /// <summary>
    /// 由Schedule调用，请不要自行调用，除非你知道自己在做什么！！！
    /// </summary>
    public static void Tick()
    {
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        time = (now - startTime) * 0.001f;
        if (lastTick == 0) lastTick = now;
        deltaTime = (now - lastTick) * 0.001f;//deltaTime是以秒作为单位的
        lastTick = now;
    }
}