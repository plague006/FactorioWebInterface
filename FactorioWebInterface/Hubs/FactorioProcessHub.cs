using FactorioWebInterface.Models;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using FactorioServerStatus = FactorioWrapperInterface.FactorioServerStatus;

namespace FactorioWebInterface.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
                string id = (string)serverId;
                Groups.RemoveFromGroupAsync(connectionId, id);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public Task RegisterServerId(string serverId)
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
                string id = (string)serverId;
                _factorioServerManger.FactorioDataReceived(id, data);
            }

            return Task.FromResult(0);
        }

        public Task SendWrapperData(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                _factorioServerManger.FactorioWrapperDataReceived(id, data);
            }

            return Task.FromResult(0);
        }

        public Task StatusChanged(FactorioServerStatus newStatus, FactorioServerStatus oldStatus)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return _factorioServerManger.StatusChanged(id, newStatus, oldStatus);
            }

            return Task.FromResult(0);
        }
    }
}

