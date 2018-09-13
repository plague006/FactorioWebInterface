using System;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    public interface IServerHub
    {
        event EventHandler<ServerHubEventArgs> FactorioDataReceived;
        event EventHandler<ServerHubEventArgs> FactorioWrapperDataReceived;

        Task SendToFactorio(string serverId, string data);
        Task Stop(string serverId);
        Task ForceStop(string serverId);
    }
}
