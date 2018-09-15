namespace FactorioWebInterface.Models
{
    public interface IFactorioServerManager
    {
        FactorioServer GetServer(int serverId);
        void StartWrapper(int serverId);
    }
}