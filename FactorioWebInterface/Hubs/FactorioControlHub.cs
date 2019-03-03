using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    [Authorize]
    public class FactorioControlHub : Hub<IFactorioControlClientMethods>, IFactorioControlServerMethods
    {
        private IFactorioServerManager _factorioServerManager;

        public FactorioControlHub(IFactorioServerManager factorioServerManager)
        {
            _factorioServerManager = factorioServerManager;
        }

        public async Task<FactorioContorlClientData> SetServerId(string serverId)
        {
            string connectionId = Context.ConnectionId;
            Context.Items[connectionId] = serverId;

            await Groups.RemoveFromGroupAsync(connectionId, serverId);
            await Groups.AddToGroupAsync(connectionId, serverId);

            return new FactorioContorlClientData()
            {
                Status = (await _factorioServerManager.GetStatus(serverId)).ToString(),
                Messages = await _factorioServerManager.GetFactorioControlMessagesAsync(serverId)
            };
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                Groups.RemoveFromGroupAsync(connectionId, id);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public Task<Result> ForceStop()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.ForceStop(id, name);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task GetStatus()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return _factorioServerManager.RequestStatus(id);
            }
            else
            {
                // todo log error.
                return Task.FromResult(0);
            }
        }

        public Task<Result> Load(string directoryName, string fileName)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string userName = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.Load(id, directoryName, fileName, userName);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task SendToFactorio(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string userName = Context.User.Identity.Name;
                string id = (string)serverId;
                _factorioServerManager.FactorioControlDataReceived(id, data, userName);
            }

            return Task.FromResult(0);
        }

        public Task<Result> Resume()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.Resume(id, name);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task<Result> StartScenario(string scenarioName)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.StartScenario(id, scenarioName, name);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task<Result> Stop()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.Stop(id, name);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task<MessageData[]> GetMesssages()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return _factorioServerManager.GetFactorioControlMessagesAsync(id);
            }

            return Task.FromResult(new MessageData[0]);
        }

        public Task<FileMetaData[]> GetTempSaveFiles()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                var files = _factorioServerManager.GetTempSaveFiles(id);
                return Task.FromResult(files);
            }

            return Task.FromResult(new FileMetaData[0]);
        }

        public Task<FileMetaData[]> GetLocalSaveFiles()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                var files = _factorioServerManager.GetLocalSaveFiles(id);
                return Task.FromResult(files);
            }

            return Task.FromResult(new FileMetaData[0]);
        }

        public Task<FileMetaData[]> GetGlobalSaveFiles()
        {
            return Task.FromResult(_factorioServerManager.GetGlobalSaveFiles());
        }

        public Task<List<FileMetaData>> GetLogFiles()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                var files = _factorioServerManager.GetLogs(id);
                return Task.FromResult(files);
            }

            return Task.FromResult(new List<FileMetaData>());
        }

        public Task<List<FileMetaData>> GetChatLogFiles()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                var files = _factorioServerManager.GetChatLogs(id);
                return Task.FromResult(files);
            }

            return Task.FromResult(new List<FileMetaData>());
        }

        public Task<Result> DeleteFiles(List<string> filePaths)
        {
            if (filePaths == null)
            {
                return Task.FromResult(Result.Failure(Constants.MissingFileErrorKey, "No file."));
            }

            return Task.FromResult(_factorioServerManager.DeleteFiles(filePaths));
        }

        public Task<Result> MoveFiles(string destination, List<string> filePaths)
        {
            if (destination == null)
            {
                return Task.FromResult(Result.Failure(Constants.FileErrorKey, "Invalid destination."));
            }

            if (filePaths == null)
            {
                return Task.FromResult(Result.Failure(Constants.MissingFileErrorKey, "No file."));
            }

            return Task.FromResult(_factorioServerManager.MoveFiles(destination, filePaths));
        }

        public Task<Result> CopyFiles(string destination, List<string> filePaths)
        {
            if (destination == null)
            {
                return Task.FromResult(Result.Failure(Constants.FileErrorKey, "Invalid destination."));
            }

            if (filePaths == null)
            {
                return Task.FromResult(Result.Failure(Constants.MissingFileErrorKey, "No file."));
            }

            return _factorioServerManager.CopyFiles(destination, filePaths);
        }

        public Task<Result> RenameFile(string directoryPath, string fileName, string newFileName)
        {
            if (directoryPath == null || fileName == null || newFileName == null)
            {
                return Task.FromResult(Result.Failure(Constants.FileErrorKey, "Invalid file."));
            }

            return Task.FromResult(_factorioServerManager.RenameFile(directoryPath, fileName, newFileName));
        }

        public Task<FactorioServerSettingsWebEditable> GetServerSettings()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return _factorioServerManager.GetEditableServerSettings(id);
            }

            return Task.FromResult(new FactorioServerSettingsWebEditable());
        }

        public async Task<Result> SaveServerSettings(FactorioServerSettingsWebEditable settings)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return await _factorioServerManager.SaveEditableServerSettings(id, settings);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return error;
        }

        public Task<FactorioServerExtraSettings> GetServerExtraSettings()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return _factorioServerManager.GetExtraServerSettings(id);
            }

            return Task.FromResult(new FactorioServerExtraSettings());
        }

        public async Task<Result> SaveServerExtraSettings(FactorioServerExtraSettings settings)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {

                string id = (string)serverId;
                return await _factorioServerManager.SaveExtraServerSettings(id, settings);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return error;
        }

        public Task<Result> Save()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.Save(id, name, "currently-running.zip");
            }
            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task<ScenarioMetaData[]> GetScenarios()
        {
            return Task.FromResult(_factorioServerManager.GetScenarios());
        }

        public Task<Result> DeflateSave(string directoryPath, string fileName, string newFileName)
        {
            return Task.FromResult(_factorioServerManager.DeflateSave(Context.ConnectionId, directoryPath, fileName, newFileName));
        }

        public async Task<Result> Update(string version = "latest")
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return await _factorioServerManager.Install(id, name, version);
            }
            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return error;
        }

        public Task RequestGetDownloadableVersions()
        {
            var client = Clients.Client(Context.ConnectionId);

            _ = Task.Run(async () =>
             {
                 var result = await _factorioServerManager.GetDownloadableVersions();
                 _ = client.SendDownloadableVersions(result);
             });

            return Task.FromResult(0);
        }

        public Task RequestGetCachedVersions()
        {
            var client = Clients.Client(Context.ConnectionId);

            _ = Task.Run(async () =>
            {
                var result = await _factorioServerManager.GetCachedVersions();
                _ = client.SendCachedVersions(result);
            });

            return Task.FromResult(0);
        }

        public Task DeleteCachedVersion(string version)
        {
            var client = Clients.Client(Context.ConnectionId);

            _ = Task.Run(async () =>
            {
                _ = _factorioServerManager.DeleteCachedVersion(version);

                var result = await _factorioServerManager.GetCachedVersions();
                _ = client.SendCachedVersions(result);
            });

            return Task.FromResult(0);
        }

        public Task<string> GetVersion()
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                return Task.FromResult(_factorioServerManager.GetVersion(id));
            }

            return Task.FromResult("");
        }
    }
}
