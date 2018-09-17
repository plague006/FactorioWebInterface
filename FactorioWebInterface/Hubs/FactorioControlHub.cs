using FactorioWebInterface.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    public class FactorioControlHub : Hub<IFactorioControlClientMethods>, IFactorioControlServerMethods
    {
        private IFactorioServerManager _factorioServerManager;

        public FactorioControlHub(IFactorioServerManager factorioServerManager)
        {
            _factorioServerManager = factorioServerManager;
        }

        public async Task SetServerId(int serverId)
        {
            string connectionId = Context.ConnectionId;
            Context.Items[connectionId] = serverId;

            await Groups.RemoveFromGroupAsync(connectionId, serverId.ToString());
            await Groups.AddToGroupAsync(connectionId, serverId.ToString());
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                Groups.RemoveFromGroupAsync(connectionId, id.ToString());
            }
            return base.OnDisconnectedAsync(exception);
        }

        public Task ForceStop()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                _factorioServerManager.ForceStop(id);
            }

            return Task.FromResult(0);
        }

        public Task<FactorioServerStatus> GetStatus()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                var status = _factorioServerManager.GetStatus(id);
                return Task.FromResult(status);
            }
            else
            {
                // todo throw error?
                return Task.FromResult(FactorioServerStatus.Unknown);
            }
        }

        public Task Load(string saveFilePath)
        {
            throw new NotImplementedException();
        }

        public Task SendToFactorio(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                _factorioServerManager.SendToFactorio(id, data);
            }

            return Task.FromResult(0);
        }

        public Task Start()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                _factorioServerManager.Start(id);
            }

            return Task.FromResult(0);
        }

        public Task Stop()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                _factorioServerManager.Stop(id);
            }

            return Task.FromResult(0);
        }
    }
}
