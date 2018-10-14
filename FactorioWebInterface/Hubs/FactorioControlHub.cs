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

        public Task<Result> Load(string saveFilePath)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string name = Context.User.Identity.Name;
                string id = (string)serverId;
                return _factorioServerManager.Load(id, saveFilePath, name);
            }

            var error = Result.Failure(Constants.ServerIdErrorKey, $"The server id for the connection is invalid.");
            return Task.FromResult(error);
        }

        public Task SendToFactorio(string data)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object serverId))
            {
                string id = (string)serverId;
                _factorioServerManager.SendToFactorioProcess(id, data);
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
    }
}
