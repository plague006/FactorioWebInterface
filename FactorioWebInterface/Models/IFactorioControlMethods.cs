using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IFactorioControlServerMethods
    {
        Task Start(int serverId);
        Task Load(int serverId, string saveFilePath);
        Task Stop(int serverId);
        Task ForceStop(int serverId);
        Task GetStatus(int serverId);
        Task SendToFactorio(int serverId, string data);
    }

    public interface IFactorioControlClientMethods
    {
        Task FactorioOutputData(int serverId, string data);
        Task FactorioWrapperOutputData(int serverId, string data);
        Task FactorioWebInterfaceData(int serverId, string data);
    }
}
