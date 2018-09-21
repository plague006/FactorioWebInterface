using System.Threading.Tasks;

namespace FactorioWrapperInterface
{
    public enum FactorioServerStatus
    {
        Unknown,
        Stopped,
        Crashed,
        Stopping,
        Starting,
        Killing,
        Killed,
        Updating,
        Updated,
        Running,
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
