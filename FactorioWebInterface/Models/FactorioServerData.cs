using System.Collections.Generic;

namespace FactorioWebInterface.Models
{
    public enum FactorioServerStatus
    {
        Unknown,
        Stopped,
        Crashed,
        Stopping,
        Starting,
        Killing,
        Killed,
        Updating,
        Updated,
        Running,
    }
    public class FactorioServerData
    {
        public int ServerId { get; set; }
        public FactorioServerStatus Status { get; set; }
        public string BaseDirectoryPath { get; set; }

        public static Dictionary<int, FactorioServerData> Servers = new Dictionary<int, FactorioServerData>()
        {
            [1] = new FactorioServerData() { ServerId = 1, Status = FactorioServerStatus.Stopped, BaseDirectoryPath = "/factorio/server1/" },
            [2] = new FactorioServerData() { ServerId = 2, Status = FactorioServerStatus.Stopped, BaseDirectoryPath = "/factorio/server2/" },
            [3] = new FactorioServerData() { ServerId = 3, Status = FactorioServerStatus.Stopped, BaseDirectoryPath = "/factorio/server3/" },
            [4] = new FactorioServerData() { ServerId = 4, Status = FactorioServerStatus.Stopped, BaseDirectoryPath = "/factorio/server4/" },
        };
    }
}
