using FactorioWebInterface.Data;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IFactorioServerManager
    {
        Task<Result> Resume(string serverId, string userName);
        Task<Result> Load(string serverId, string directoryName, string fileName, string userName);
        Task<Result> StartScenario(string serverId, string scenarioName, string userName);
        Task<Result> Stop(string serverId, string userName);
        Task<Result> ForceStop(string serverId, string userName);
        Task<FactorioServerStatus> GetStatus(string serverId);
        Task RequestStatus(string serverId);
        Task<MessageData[]> GetFactorioControlMessagesAsync(string serverId);
        Task SendToFactorioProcess(string serverId, string data);
        Task FactorioDataReceived(string serverId, string data);
        Task FactorioControlDataReceived(string serverId, string data, string userName);
        void FactorioWrapperDataReceived(string serverId, string data);
        Task OnProcessRegistered(string serverId);
        Task StatusChanged(string serverId, FactorioServerStatus newStatus, FactorioServerStatus oldStatus);
        Task<List<Regular>> GetRegularsAsync();
        Task<List<Ban>> GetBansAsync();
        Task<List<Admin>> GetAdminsAsync();
        Task AddRegularsFromStringAsync(string data);
        Task AddAdminsFromStringAsync(string data);
        FileMetaData[] GetLocalSaveFiles(string serverId);
        FileMetaData[] GetTempSaveFiles(string serverId);
        FileMetaData[] GetGlobalSaveFiles();
        ScenarioMetaData[] GetScenarios();
        List<FileMetaData> GetLogs(string serverId);
        FileInfo GetLogFile(string directoryName, string fileName);
        FileInfo GetSaveFile(string directory, string fileName);
        Task<Result> UploadFiles(string directory, IList<IFormFile> files);
        Result DeleteFiles(List<string> filePaths);
        Result MoveFiles(string destination, List<string> filePaths);
        Task<Result> CopyFiles(string destination, List<string> filePaths);
        Result RenameFile(string directoryPath, string fileName, string newFileName);
        //Task ReloadServerSettings(string serverId);
        Task<FactorioServerSettingsWebEditable> GetEditableServerSettings(string serverId);
        Task<Result> SaveEditableServerSettings(string serverId, FactorioServerSettingsWebEditable settings);
        Task<Result> Install(string id, string userName, string version);
        Task<Result> Save(string id, string userName, string saveName);
        Result DeflateSave(string directoryPath, string fileName, string newFileName);
    }
}