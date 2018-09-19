namespace FactorioWebInterface.Models
{
    public class ServerMessageEventArgs
    {
        public ServerMessageEventArgs(string serverId, string data)
        {
            ServerId = serverId;
            Data = data;
        }

        public string ServerId { get; }
        public string Data { get; }
    }
}