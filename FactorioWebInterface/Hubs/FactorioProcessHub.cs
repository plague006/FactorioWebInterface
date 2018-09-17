using FactorioWebInterface.Models;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    [AllowAnonymous]
    public class FactorioProcessHub : Hub<IFactorioProcessClientMethods>, IFactorioProcessServerMethods
    {
        private readonly IFactorioServerManager _factorioServerManger;

        public FactorioProcessHub(IFactorioServerManager factorioServerManger)
        {
            _factorioServerManger = factorioServerManger;
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

        public Task RegisterServerId(int serverId)
        {
            string connectionId = Context.ConnectionId;
            Context.Items[connectionId] = serverId;

            return Groups.AddToGroupAsync(connectionId, serverId.ToString());           
        }

        public Task SendFactorioOutputData(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                _factorioServerManger.FactorioDataReceived(id, data);
            }

            return Task.FromResult(0);
        }

        public Task SendWrapperData(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                int id = (int)serverId;
                _factorioServerManger.FactorioWrapperDataReceived(id, data);
            }

            return Task.FromResult(0);
        }
    }
}

