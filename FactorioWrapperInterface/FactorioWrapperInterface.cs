using System.Threading.Tasks;

namespace FactorioWrapperInterface
{
    public interface IClientMethods
    {
        Task SendToFactorio(string data);
        Task Stop();
        Task ForceStop();
    }

    public interface IServerMethods
    {
        Task SendFactorioOutputData(string serverId, string data);
        Task SendWrapperData(string serverId, string data);
    }
}
