namespace FactorioWebInterface.Models
{
    public class FactorioProcessRelayEventArgs
    {
        public FactorioProcessRelayEventArgs(int serverId, string data)
        {
            ServerId = serverId;
            Data = data;
        }

        public int ServerId { get; }
        public string Data { get; }
    }
}
