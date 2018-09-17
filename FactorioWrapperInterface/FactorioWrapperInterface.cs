using System.Threading.Tasks;

namespace FactorioWrapperInterface
{
    public interface IFactorioProcessClientMethods
    {
        Task SendToFactorio(string data);
        Task Stop();
        Task ForceStop();
    }

    public interface IFactorioProcessServerMethods
    {
        Task RegisterServerId(int serverId);
        Task SendFactorioOutputData(string data);
        Task SendWrapperData(string data);
    }
}
