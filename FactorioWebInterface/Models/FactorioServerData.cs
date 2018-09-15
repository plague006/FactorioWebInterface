namespace FactorioWebInterface.Models
{
    public enum FactorioServerStatus
    {
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
    public class FactorioServerData
    {
        public int ServerId { get; set; }
        public FactorioServerStatus Status { get; set; }
    }
}
