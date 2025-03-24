using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Proto;
using Serilog;

namespace Test;

public class Program
{
    static void Main(string[] args)
    {
        HeartBeatResponse heartBeatResponse = new HeartBeatResponse();

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
            int lengthComparison = x.Length.CompareTo(y.Length);
            return lengthComparison != 0 ? lengthComparison : string.Compare(x, y, StringComparison.Ordinal);
        });

        for (int i = 0; i < list1.Count; i++)
        {
            var fname = list1[i];
            //Log.Debug("Proto类型注册：{0}  {1}", i, fname);
        }
        

        Log.Debug("Proto协议加载完成,共加载{0}个", list1.Count);
    }
}