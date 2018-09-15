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
        Task RegisterServerId(string serverId);
        Task SendFactorioOutputData(string data);
        Task SendWrapperData(string data);
    }
}
