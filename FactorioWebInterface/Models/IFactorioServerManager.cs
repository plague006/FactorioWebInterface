using FactorioWebInterface.Data;
using FactorioWrapperInterface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IFactorioServerManager
    {
        bool Start(string serverId);
        bool Load(string serverId, string saveFilePath);
        void Stop(string serverId);
        void ForceStop(string serverId);
        Task<FactorioServerStatus> GetStatus(string serverId);
        Task RequestStatus(string serverId);
        Task<MessageData[]> GetFactorioControlMessagesAsync(string serverId);
        Task SendToFactorioProcess(string serverId, string data);
        void FactorioDataReceived(string serverId, string data);
        void FactorioWrapperDataReceived(string serverId, string data);
        Task OnProcessRegistered(string serverId);
        Task StatusChanged(string serverId, FactorioServerStatus newStatus, FactorioServerStatus oldStatus);
        Task<List<Regular>> GetRegularsAsync();
        Task AddRegularsFromStringAsync(string data);
        FileData[] GetLocalSaveFiles(string serverId);
    }
}