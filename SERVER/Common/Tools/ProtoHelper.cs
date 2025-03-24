using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Serilog;

namespace Common;

/// <summary>
/// Protobuf序列化与反序列化
/// </summary>
public class ProtoHelper
{
    /// <summary>
    /// 字典用于保存message的所有类型，用于拆包时进行类型转换
    /// key：message的全限定名， value：message的类型
    /// </summary>
    private static Dictionary<string, Type> _registry = new Dictionary<string, Type>();

    /// <summary>
    /// 用于保存协议类型和协议id的映射关系
    /// </summary>
    private static Dictionary<int, Type> mDict1 = new Dictionary<int, Type>();

    private static Dictionary<Type, int> mDict2 = new Dictionary<Type, int>();
    public static int CodeCount => mDict1.Count;

    /// <summary>
    /// 根据类型获取协议在中网络传输的id值
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int SeqCode(Type type)
    {
        return mDict2[type];
    }

    /// <summary>
    /// 根据协议在中网络传输的id值获取协议的类型
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static Type SeqType(int code)
    {
        return mDict1[code];
    }

    static ProtoHelper()
    {
        //  LINQ 查询语法，获取当前正在执行的程序集中的所有类型。
        //找到Message程序集
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //找到Message程序集

        var list1 = assemblies.Where(a => a.FullName.Contains("Message"))
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IMessage).IsAssignableFrom(t))
            .Select(t => t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor)
            .Where(desc => desc != null)
            .Select(desc => desc.FullName)
            .ToList();
        //根据协议名的字符串进行排序
        list1.Sort((x, y) =>
        {
            //根据字符串长度排序
            if (x.Length != y.Length)
            {
                return x.Length - y.Length;
            }

            //如果长度相同
            //则使用x和y基于 Unicode码点值的排序规则进行字符串比较，保证了排序的稳定性(大白话就算对应的整型值，x<y就返回负数)
            return string.Compare(x, y, StringComparison.Ordinal);
        });

        for (int i = 0; i < list1.Count; i++)
        {
            var fname = list1[i];
            //Log.Debug("Proto类型注册：{0}  {1}", i,fname);
            var t = _registry[fname];
            mDict1.Add(i, t);
            mDict2.Add(t, i);
        }

        Log.Information("Proto协议加载完成,共加载{0}个", list1.Count);
    }

    /// <summary>
    /// 根据协议在中网络传输的id值解析成一个imassage
    /// </summary>
    /// <param name="typeCode"></param>
    /// <param name="data"></param>
    /// <param name="offset"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    public static IMessage ParseFrom(int code, byte[] data, int i, int dataLength)
    {
        Type t = ProtoHelper.SeqType(code);
        var desc = t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;
        var msg = desc.Parser.ParseFrom(data, 2, data.Length - 2);
        return msg;
    }

    public static void Init()
    {
        
    }
}