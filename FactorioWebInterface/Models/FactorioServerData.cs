using DSharpPlus;
using FactorioWrapperInterface;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FactorioWebInterface.Models
{
    public class FactorioServerData
    {
        //private static string baseDirectoryPath = "/factorio/";
        public static string baseDirectoryPath = "C:/factorio/";

        public static readonly int serverCount = 6;
        public static readonly int bufferSize = 100;

        public static string GlobalSavesDirectoryPath { get; } = Path.Combine(baseDirectoryPath, Constants.GlobalSavesDirectoryName);

        public string ServerId { get; set; }
        public FactorioServerStatus Status { get; set; }
        public string BaseDirectoryPath { get; set; }
        public string TempSavesDirectoryPath { get; set; }
        public string LocalSavesDirectoroyPath { get; set; }
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

                string basePath = $"{baseDirectoryPath}{serverId}/";
                Servers[serverId] = new FactorioServerData()
                {
                    ServerId = serverId,
                    Status = FactorioServerStatus.Unknown,
                    BaseDirectoryPath = baseDirectoryPath,
                    TempSavesDirectoryPath = Path.Combine(basePath, Constants.TempSavesDirectoryName),
                    LocalSavesDirectoroyPath = Path.Combine(basePath, Constants.LocalSavesDirectoryName),
                    Port = port,
                    ServerLock = new SemaphoreSlim(1, 1),
                    ControlMessageBuffer = new RingBuffer<MessageData>(bufferSize)
                };
            }
        }
    }
}
