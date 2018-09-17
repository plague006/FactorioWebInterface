namespace FactorioWebInterface.Models
{
    public interface IFactorioServerManager
    {
        bool Start(int serverId);
        bool Load(int serverId, string saveFilePath);
        void Stop(int serverId);
        void ForceStop(int serverId);
        FactorioServerStatus GetStatus(int serverId);
        void SendToFactorio(int serverId, string data);
        void FactorioDataReceived(int serverId, string data);
        void FactorioWrapperDataReceived(int serverId, string data);
    }
}