using FactorioWebInterface.Utils;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IFactorioProcessRelay
    {
        event EventHandler<IFactorioProcessRelay, FactorioProcessRelayEventArgs> FactorioDataReceived;
        event EventHandler<IFactorioProcessRelay, FactorioProcessRelayEventArgs> FactorioWrapperDataReceived;

        void RaiseFactorioDataReceived(string serverId, string data);
        void RaiseFactorioWrapperDataReceived(string serverId, string data);

        void ServerConnected(string serverId, string connectionId);
        void ServerDisconnected(string serverId, string connectionId);

        Task SendToFactorio(string serverId, string data);
        Task Stop(string serverId);
        Task ForceStop(string serverId);
    }
}
