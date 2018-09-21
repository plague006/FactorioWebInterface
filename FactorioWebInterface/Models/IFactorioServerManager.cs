using FactorioWrapperInterface;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IFactorioServerManager
    {
        bool Start(string serverId);
        bool Load(string serverId, string saveFilePath);
        void Stop(string serverId);
        void ForceStop(string serverId);
        FactorioServerStatus GetStatus(string serverId);
        void SendToFactorio(string serverId, string data);
        void FactorioDataReceived(string serverId, string data);
        void FactorioWrapperDataReceived(string serverId, string data);
        Task StatusChanged(string serverId, FactorioServerStatus newStatus, FactorioServerStatus oldStatus);
    }
}