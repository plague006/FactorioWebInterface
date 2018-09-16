using FactorioWebInterface.Models;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    [AllowAnonymous]
    public class FactorioProcessHub : Hub<IFactorioProcessClientMethods>, IFactorioProcessServerMethods
    {
        private IFactorioProcessRelay _factorioProcessRelay;

        public FactorioProcessHub(IFactorioProcessRelay factorioProcessRelay)
        {
            _factorioProcessRelay = factorioProcessRelay;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                _factorioProcessRelay.ServerDisconnected(id, connectionId);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public Task RegisterServerId(string serverId)
        {
            string connectionId = Context.ConnectionId;
            Context.Items[connectionId] = serverId;

            _factorioProcessRelay.ServerConnected(serverId, connectionId);
            return Task.FromResult(0);
        }

        public Task SendFactorioOutputData(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                Debug.WriteLine($"serverId: {id}, data: {data}");
                _factorioProcessRelay.RaiseFactorioDataReceived(id, data);
            }

            return Task.FromResult(0);
        }

        public Task SendWrapperData(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                Debug.WriteLine($"serverId: {id}, data: {data}");
                _factorioProcessRelay.RaiseFactorioWrapperDataReceived(id, data);
            }

            return Task.FromResult(0);
        }
    }
}

