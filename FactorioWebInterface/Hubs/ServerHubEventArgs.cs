namespace FactorioWebInterface.Hubs
{
    public class ServerHubEventArgs
    {
        public ServerHubEventArgs(string serverId, string data)
        {
            ServerId = serverId;
            Data = data;
        }

        public string ServerId { get; }
        public string Data { get; }
    }
}
