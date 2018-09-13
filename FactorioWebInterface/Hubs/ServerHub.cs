using FactorioWrapperInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    [AllowAnonymous]
    public class ServerHub : Hub<IClientMethods>, IServerHub
    {
        public ServerHub()
        {

        }

        public event EventHandler<ServerHubEventArgs> FactorioDataReceived;
        public event EventHandler<ServerHubEventArgs> FactorioWrapperDataReceived;

        public Task SendFactorioOutputData(string serverId, string data)
        {
            Debug.WriteLine($"serverId: {serverId}, data: {data}");

            var e = FactorioDataReceived;
            if (e != null)
            {
                FactorioDataReceived(this, new ServerHubEventArgs(serverId, data));
            }

            return Task.FromResult(0);
        }

        public Task SendWrapperData(string serverId, string data)
        {
            Debug.WriteLine($"serverId: {serverId}, data: {data}");

            var e = FactorioWrapperDataReceived;
            if (e != null)
            {
                FactorioDataReceived(this, new ServerHubEventArgs(serverId, data));
            }

            return Task.FromResult(0);
        }

        public Task SendToFactorio(string serverId, string data)
        {
            Clients.All.SendToFactorio(data);

            return Task.FromResult(0);
        }

        public Task Stop(string serverId)
        {
            Clients.All.Stop();

            return Task.FromResult(0);
        }

        public Task ForceStop(string serverId)
        {
            Clients.All.ForceStop();

            return Task.FromResult(0);
        }
    }
}
