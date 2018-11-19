using DSharpPlus;
using FactorioWrapperInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServerData
    {
        public static readonly string baseDirectoryPath = Path.GetFullPath("/factorio/");
        public static readonly string basePublicDirectoryPath = Path.GetFullPath("/factorio/public/");

        public static readonly int serverCount = 6;
        public static readonly int bufferSize = 100;
        public static readonly int maxLogFiles = 20;

        public static string GlobalSavesDirectoryPath { get; } = Path.GetFullPath(Path.Combine(baseDirectoryPath, Constants.GlobalSavesDirectoryName));
        public static string ScenarioDirectoryPath { get; } = Path.GetFullPath(Path.Combine(baseDirectoryPath, Constants.ScenarioDirectoryName));

        public static HashSet<string> ValidSaveDirectories { get; } = new HashSet<string>();

        public string ServerId { get; set; }
        public FactorioServerStatus Status { get; set; }
        public string BaseDirectoryPath { get; set; }
        public string TempSavesDirectoryPath { get; set; }
        public string LocalSavesDirectoroyPath { get; set; }
        public string LocalScenarioDirectoryPath { get; set; }
        public string LogsDirectoryPath { get; set; }
        public string CurrentLogPath { get; set; }
        public string ServerSettingsPath { get; set; }
        public string ServerBanListPath { get; set; }
        public string Port { get; set; }
        public SemaphoreSlim ServerLock { get; set; }
        public RingBuffer<MessageData> ControlMessageBuffer { get; set; }
        public FactorioServerSettings ServerSettings { get; set; }

        public Func<Task> StopCallback { get; set; }

        public static Dictionary<string, FactorioServerData> Servers { get; }

        static FactorioServerData()
        {
            ValidSaveDirectories.Add(Constants.GlobalSavesDirectoryName);
            ValidSaveDirectories.Add(Constants.PublicStartSavesDirectoryName);
            ValidSaveDirectories.Add(Constants.PublicFinalSavesDirectoryName);
            ValidSaveDirectories.Add(Constants.PublicOldSavesDirectoryName);
            ValidSaveDirectories.Add(Constants.WindowsPublicStartSavesDirectoryName);
            ValidSaveDirectories.Add(Constants.WindowsPublicFinalSavesDirectoryName);
            ValidSaveDirectories.Add(Constants.WindowsPublicOldSavesDirectoryName);

            Servers = new Dictionary<string, FactorioServerData>();

            for (int i = 1; i <= serverCount; i++)
            {
                string port = (34200 + i).ToString();
                string serverId = i.ToString();

                string basePath = Path.Combine(baseDirectoryPath, serverId);
                Servers[serverId] = new FactorioServerData()
                {
                    ServerId = serverId,
                    Status = FactorioServerStatus.Unknown,
                    BaseDirectoryPath = basePath,
                    TempSavesDirectoryPath = Path.Combine(basePath, Constants.TempSavesDirectoryName),
                    LocalSavesDirectoroyPath = Path.Combine(basePath, Constants.LocalSavesDirectoryName),
                    ServerSettingsPath = Path.Combine(basePath, Constants.ServerSettingsFileName),
                    LocalScenarioDirectoryPath = Path.Combine(basePath, Constants.ScenarioDirectoryName),
                    LogsDirectoryPath = Path.Combine(basePath, Constants.LogDirectoryName),
                    CurrentLogPath = Path.Combine(basePath, Constants.CurrentLogFileName),
                    ServerBanListPath = Path.Combine(basePath, Constants.ServerBanListFileName),
                    Port = port,
                    ServerLock = new SemaphoreSlim(1, 1),
                    ControlMessageBuffer = new RingBuffer<MessageData>(bufferSize)
                };

                ValidSaveDirectories.Add($"{serverId}/{Constants.TempSavesDirectoryName}");
                ValidSaveDirectories.Add($"{serverId}/{Constants.LocalSavesDirectoryName}");
                ValidSaveDirectories.Add($"{serverId}\\{Constants.TempSavesDirectoryName}");
                ValidSaveDirectories.Add($"{serverId}\\{Constants.LocalSavesDirectoryName}");
            }
        }
    }
}
