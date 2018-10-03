using DSharpPlus;
using FactorioWrapperInterface;
using System.Collections.Generic;
using System.Threading;

namespace FactorioWebInterface.Models
{
    public class FactorioServerData
    {
        public static readonly int serverCount = 6;
        public static readonly int bufferSize = 100;
        //private static string baseDirectoryPath = "/factorio/";
        private static string baseDirectoryPath = "C:/factorio/";

        public string ServerId { get; set; }
        public FactorioServerStatus Status { get; set; }
        public string BaseDirectoryPath { get; set; }
        public string Port { get; set; }
        public SemaphoreSlim ServerLock { get; set; }
        public RingBuffer<MessageData> ControlMessageBuffer { get; set; }

        public static Dictionary<string, FactorioServerData> Servers { get; }

        static FactorioServerData()
        {
            Servers = new Dictionary<string, FactorioServerData>();

            for (int i = 1; i <= serverCount; i++)
            {
                string port = (34200 + i).ToString();
                string serverId = i.ToString();
                Servers[serverId] = new FactorioServerData()
                {
                    ServerId = serverId,
                    Status = FactorioServerStatus.Unknown,
                    BaseDirectoryPath = $"{baseDirectoryPath}{serverId}/",
                    Port = port,
                    ServerLock = new SemaphoreSlim(1, 1),
                    ControlMessageBuffer = new RingBuffer<MessageData>(bufferSize)
                };
            }
        }
    }
}
