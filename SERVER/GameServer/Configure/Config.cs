﻿using YamlDotNet.Serialization;

namespace GameServer;

    public class ServerConfig
    {
        [YamlMember(Alias = "gameWorldId")]
        public int gameWorldId;

        [YamlMember(Alias = "ip")]
        public string ip { get; set; }

        [YamlMember(Alias = "serverPort")]
        public int serverPort { get; set; }

        [YamlMember(Alias = "workerCount")]
        public int workerCount { get; set; }        
        
        [YamlMember(Alias = "aoiViewArea")]
        public float aoiViewArea { get; set; }        
        
        [YamlMember(Alias = "updateHz")]
        public int updateHz { get; set; }

        [YamlMember(Alias = "heartBeatTimeOut")]
        public float heartBeatTimeOut { get; set; }

        [YamlMember(Alias = "heartBeatCheckInterval")]
        public float heartBeatCheckInterval { get; set; }

        [YamlMember(Alias = "heartBeatSendInterval")]
        public float heartBeatSendInterval { get; set; }
    }

    public class CCConfig
    {
        [YamlMember(Alias = "ip")]
        public string ip { get; set; }

        [YamlMember(Alias = "port")]
        public int port { get; set; }
    }

    public class AppConfig
    {

        [YamlMember(Alias = "server")]
        public ServerConfig Server { get; set; }

        [YamlMember(Alias = "cc")]
        public CCConfig CCServer { get; set; }
    }

    public static class Config
    {
        private static AppConfig _config;

        public static void Init(string filePath = "config.yaml")
        {
            // 读取配置文件,当前项目根目录
            var yaml = File.ReadAllText(filePath);
            //Log.Information("LoadYamlText:\r\n {Yaml}", yaml);

            // 反序列化配置文件
            var deserializer = new DeserializerBuilder().Build();
            _config = deserializer.Deserialize<AppConfig>(yaml);
        }
        
        public static ServerConfig Server => _config?.Server;
        public static CCConfig CCConfig => _config?.CCServer;
    }