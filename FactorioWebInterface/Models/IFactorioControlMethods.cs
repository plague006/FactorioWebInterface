using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public enum MessageType
    {
        Output,
        Wrapper,
        Control,
        Status,
        Discord
    }

    public class MessageData
    {
        public MessageType MessageType { get; set; }
        public string Message { get; set; }
    }

    public class FactorioContorlClientData
    {
        public string Status { get; set; }
        public MessageData[] Messages { get; set; }
    }

    public interface IFactorioControlServerMethods
    {
        Task<FactorioContorlClientData> SetServerId(string serverId);
        Task Start();
        Task Load(string saveFilePath);
        Task Stop();
        Task ForceStop();
        Task GetStatus();
        Task<MessageData[]> GetMesssages();
        Task SendToFactorio(string data);
        Task<FileMetaData[]> GetTempSaveFiles();
        Task<FileMetaData[]> GetLocalSaveFiles();
        Task<FileMetaData[]> GetGlobalSaveFiles();
    }

    public interface IFactorioControlClientMethods
    {
        //Task FactorioOutputData(string data);
        //Task FactorioWrapperOutputData(string data);
        //Task FactorioWebInterfaceData(string data);
        Task SendMessage(MessageData message);
        Task FactorioStatusChanged(string newStatus, string oldStatus);
    }
}
