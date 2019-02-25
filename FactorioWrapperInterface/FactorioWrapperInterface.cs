using System;
using System.Threading.Tasks;

namespace Shared
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
        Task RegisterServerIdWithDateTime(string serverId, DateTime dateTime);
        Task SendFactorioOutputData(string data);
        Task SendFactorioOutputDataWithDateTime(string data, DateTime dateTime);
        Task SendWrapperData(string data);
        Task SendWrapperDataWithDateTime(string data, DateTime dateTime);
        Task StatusChanged(FactorioServerStatus newStatus, FactorioServerStatus oldStatus);
        Task StatusChangedWithDateTime(FactorioServerStatus newStatus, FactorioServerStatus oldStatus, DateTime dateTime);
    }
}
