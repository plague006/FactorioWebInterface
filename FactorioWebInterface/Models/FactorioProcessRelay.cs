using FactorioWebInterface.Hubs;
using FactorioWebInterface.Utils;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioProcessRelay : IFactorioProcessRelay
    {
        private ConcurrentDictionary<string, string> serverToConnectionId = new ConcurrentDictionary<string, string>();

        private IHubContext<FactorioProcessHub, IClientMethods> _factorioProcessHub;

        public FactorioProcessRelay(IHubContext<FactorioProcessHub, IClientMethods> factorioProcessHub)
        {
            _factorioProcessHub = factorioProcessHub;
        }

        public event EventHandler<IFactorioProcessRelay, FactorioProcessRelayEventArgs> FactorioDataReceived;
        public event EventHandler<IFactorioProcessRelay, FactorioProcessRelayEventArgs> FactorioWrapperDataReceived;

        public Task Stop(string serverId)
        {
            string connectionId = GetConnectionId(serverId);
            if (connectionId != null)
            {
                _factorioProcessHub.Clients.Client(connectionId).Stop();
            }

            return Task.FromResult(0);
        }

        public Task ForceStop(string serverId)
        {
            string connectionId = GetConnectionId(serverId);
            if (connectionId != null)
            {
                _factorioProcessHub.Clients.Client(connectionId).ForceStop();
            }

            return Task.FromResult(0);
        }

        public Task SendToFactorio(string serverId, string data)
        {
            string connectionId = GetConnectionId(serverId);
            if (connectionId != null)
            {
                _factorioProcessHub.Clients.Client(connectionId).SendToFactorio(data);
            }

            return Task.FromResult(0);
        }

        public void RaiseFactorioDataReceived(string serverId, string data)
        {
            FactorioDataReceived?.Invoke(this, new FactorioProcessRelayEventArgs(serverId, data));
        }

        public void RaiseFactorioWrapperDataReceived(string serverId, string data)
        {
            FactorioWrapperDataReceived?.Invoke(this, new FactorioProcessRelayEventArgs(serverId, data));
        }

        public void ServerConnected(string serverId, string connectionId)
        {
            serverToConnectionId[serverId] = connectionId;
        }

        public void ServerDisconnected(string serverId, string connectionId)
        {
            serverToConnectionId.TryRemove(serverId, out var _);
        }

        private string GetConnectionId(string serverId)
        {
            serverToConnectionId.TryGetValue(serverId, out string connectionId);
            return connectionId;
        }      
    }
}
