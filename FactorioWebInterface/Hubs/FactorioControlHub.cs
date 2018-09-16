using FactorioWebInterface.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    public class FactorioControlHub : Hub<IFactorioControlClientMethods>, IFactorioControlServerMethods
    {
        public Task ForceStop(int serverId)
        {
            throw new NotImplementedException();
        }

        public Task GetStatus(int serverId)
        {
            throw new NotImplementedException();
        }

        public Task Load(int serverId, string saveFilePath)
        {
            throw new NotImplementedException();
        }

        public Task SendToFactorio(int serverId, string data)
        {
            throw new NotImplementedException();
        }

        public Task Start(int serverId)
        {
            throw new NotImplementedException();
        }

        public Task Stop(int serverId)
        {
            throw new NotImplementedException();
        }
    }
}
