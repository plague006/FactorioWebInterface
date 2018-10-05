using System.Threading.Tasks;

namespace FactorioWrapperInterface
{
    public enum FactorioServerStatus
    {
        Unknown,
        WrapperStarting,
        WrapperStarted,
        Starting,
        Running,
        Stopping,
        Stopped,
        Killing,
        Killed,
        Crashed,
        Updating,
        Updated,
    }

    public interface IFactorioProcessClientMethods
    {
        Task SendToFactorio(string data);
        Task Stop();
        Task ForceStop();
        Task GetStatus();
    }

    public interface IFactorioProcessServerMethods
    {
        Task RegisterServerId(string serverId);
        Task SendFactorioOutputData(string data);
        Task SendWrapperData(string data);
        Task StatusChanged(FactorioServerStatus newStatus, FactorioServerStatus oldStatus);
    }
}
