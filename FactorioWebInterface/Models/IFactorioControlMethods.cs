using FactorioWrapperInterface;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioContorlClientData
    {
        public string Status { get; set; }
    }

    public interface IFactorioControlServerMethods
    {
        Task<FactorioContorlClientData> SetServerId(string serverId);
        Task Start();
        Task Load(string saveFilePath);
        Task Stop();
        Task ForceStop();
        Task<string> GetStatus();
        Task SendToFactorio(string data);
    }

    public interface IFactorioControlClientMethods
    {
        Task FactorioOutputData(string data);
        Task FactorioWrapperOutputData(string data);
        Task FactorioWebInterfaceData(string data);
        Task FactorioStatusChanged(string newStatus, string oldStatus);
    }
}
