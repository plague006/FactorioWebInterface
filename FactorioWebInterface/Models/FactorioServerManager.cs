using DSharpPlus;
using DSharpPlus.Entities;
using FactorioWebInterface.Data;
using FactorioWebInterface.Hubs;
using FactorioWebInterface.Utils;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServerManager : IFactorioServerManager
    {
        // Match on first [*] and capture everything after.
        private static readonly Regex tag_regex = new Regex(@"(\[[^\[\]]+\])\s*((?:.|\s)*)\s*", RegexOptions.Compiled);

        private readonly IConfiguration _configuration;
        private readonly IDiscordBot _discordBot;
        private readonly IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> _factorioProcessHub;
        private readonly IHubContext<FactorioControlHub, IFactorioControlClientMethods> _factorioControlHub;
        private readonly DbContextFactory _dbContextFactory;
        private readonly ILogger<FactorioServerManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        //private SemaphoreSlim serverLock = new SemaphoreSlim(1, 1);
        private Dictionary<string, FactorioServerData> servers = FactorioServerData.Servers;

        public FactorioServerManager
        (
            IConfiguration configuration,
            IDiscordBot discordBot,
            IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> factorioProcessHub,
            IHubContext<FactorioControlHub, IFactorioControlClientMethods> factorioControlHub,
            DbContextFactory dbContextFactory,
            ILogger<FactorioServerManager> logger,
            IHttpClientFactory httpClientFactory
        )
        {
            _configuration = configuration;
            _discordBot = discordBot;
            _factorioProcessHub = factorioProcessHub;
            _factorioControlHub = factorioControlHub;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _discordBot.FactorioDiscordDataReceived += FactorioDiscordDataReceived;
        }

        private bool ExecuteProcess(string filename, string arguments)
        {
            _logger.LogInformation("ExecuteProcess filename: {fileName} arguments: {arguments}", filename, arguments);
            Process proc = Process.Start(filename, arguments);
            proc.WaitForExit();
            return proc.ExitCode > -1;
        }

        private Task SendControlMessageNonLocking(FactorioServerData serverData, MessageData message)
        {
            serverData.ControlMessageBuffer.Add(message);
            return _factorioControlHub.Clients.Groups(serverData.ServerId).SendMessage(message);
        }

        private Task ChangeStatusNonLocking(FactorioServerData serverData, FactorioServerStatus newStatus, string byUser = "")
        {
            var oldStatus = serverData.Status;
            serverData.Status = newStatus;

            string oldStatusString = oldStatus.ToString();
            string newStatusString = newStatus.ToString();

            MessageData message;
            if (byUser == "")
            {
                message = new MessageData()
                {
                    MessageType = MessageType.Status,
                    Message = $"[STATUS] Change from {oldStatusString} to {newStatusString}"
                };
            }
            else
            {
                message = new MessageData()
                {
                    MessageType = MessageType.Status,
                    Message = $"[STATUS] Change from {oldStatusString} to {newStatusString} by user {byUser}"
                };
            }

            var group = _factorioControlHub.Clients.Groups(serverData.ServerId);

            return Task.WhenAll(group.FactorioStatusChanged(newStatusString, oldStatusString), group.SendMessage(message));
        }

        private string SanitizeDiscordChat(string message)
        {
            StringBuilder sb = new StringBuilder(message);

            sb.Replace("'", "\\'");
            sb.Replace("\n", " ");

            return sb.ToString();
        }

        private void FactorioDiscordDataReceived(IDiscordBot sender, ServerMessageEventArgs eventArgs)
        {
            var name = SanitizeDiscordChat(eventArgs.User.Username);
            var message = SanitizeDiscordChat(eventArgs.Message);

            string data = $"/silent-command game.print('[Discord] {name}: {message}')";
            SendToFactorioProcess(eventArgs.ServerId, data);

            var messageData = new MessageData()
            {
                MessageType = MessageType.Discord,
                Message = $"[Discord] {eventArgs.User.Username}: {eventArgs.Message}"
            };

            _ = SendToFactorioControl(eventArgs.ServerId, messageData);
        }

        public async Task<Result> Resume(string serverId, string userName)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return Result.Failure(Constants.ServerIdErrorKey, $"serverId {serverId} not found.");
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                switch (serverData.Status)
                {
                    case FactorioServerStatus.Unknown:
                    case FactorioServerStatus.Stopped:
                    case FactorioServerStatus.Killed:
                    case FactorioServerStatus.Crashed:
                    case FactorioServerStatus.Updated:

                        var tempSaves = new DirectoryInfo(serverData.TempSavesDirectoryPath);
                        if (!tempSaves.EnumerateFiles("*.zip").Any())
                        {
                            return Result.Failure(Constants.MissingFileErrorKey, "No file to resume server from.");
                        }

                        string basePath = serverData.BaseDirectoryPath;

                        var startInfo = new ProcessStartInfo
                        {
#if WINDOWS
                            FileName = "C:/Program Files/dotnet/dotnet.exe",
                            Arguments = $"C:/Projects/FactorioWebInterface/FactorioWrapper/bin/Windows/netcoreapp2.1/FactorioWrapper.dll {serverId} {basePath}/bin/x64/factorio.exe --start-server-load-latest --server-settings {basePath}/server-settings.json --port {serverData.Port}",
#elif WSL
                            FileName = "/usr/bin/dotnet",
                            Arguments = $"/mnt/c/Projects/FactorioWebInterface/FactorioWrapper/bin/Wsl/netcoreapp2.1/publish/FactorioWrapper.dll {serverId} {basePath}/bin/x64/factorio --start-server-load-latest --server-settings {basePath}/server-settings.json --port {serverData.Port}",
#else
                            FileName = "/usr/bin/dotnet",
                            Arguments = $"/factorio/factorioWrapper/FactorioWrapper.dll {serverId} {basePath}/bin/x64/factorio --start-server-load-latest --server-settings {basePath}/server-settings.json --port {serverData.Port}",
#endif                           
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        try
                        {
                            Process.Start(startInfo);
                        }
                        catch (Exception)
                        {
                            _logger.LogError("Error resumeing serverId: {serverId}", serverId);
                            return Result.Failure(Constants.WrapperProcessErrorKey, "Wrapper process failed to start.");
                        }

                        _logger.LogInformation("Server resumed serverId: {serverId} user: {userName}", serverId, userName);

                        var group = _factorioControlHub.Clients.Group(serverId);
                        await group.FactorioStatusChanged(FactorioServerStatus.WrapperStarting.ToString(), serverData.Status.ToString());
                        serverData.Status = FactorioServerStatus.WrapperStarting;

                        var message = new MessageData()
                        {
                            MessageType = MessageType.Control,
                            Message = $"Server resumed by user: {userName}"
                        };

                        serverData.ControlMessageBuffer.Add(message);
                        await group.SendMessage(message);

                        return Result.OK;
                    default:
                        return Result.Failure(Constants.InvalidServerStateErrorKey, $"Cannot resume server when in state {serverData.Status}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error loading", e);
                return Result.Failure(Constants.UnexpctedErrorKey);
            }
            finally
            {
                serverData.ServerLock.Release();
            }
        }

        public async Task<Result> Load(string serverId, string saveFilePath, string userName)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return Result.Failure(Constants.ServerIdErrorKey, $"serverId {serverId} not found.");
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                switch (serverData.Status)
                {
                    case FactorioServerStatus.Unknown:
                    case FactorioServerStatus.Stopped:
                    case FactorioServerStatus.Killed:
                    case FactorioServerStatus.Crashed:
                    case FactorioServerStatus.Updated:

                        string filePath = Path.Combine(FactorioServerData.baseDirectoryPath, saveFilePath);

                        var fi = new FileInfo(filePath);
                        if (!fi.Exists)
                        {
                            return Result.Failure(Constants.MissingFileErrorKey, $"File {saveFilePath} not found.");
                        }

                        if (fi.Extension != ".zip")
                        {
                            return Result.Failure(Constants.InvalidFileTypeErrorKey, $"File {saveFilePath} is not valid save file type.");
                        }

                        switch (fi.Directory.Name)
                        {
                            case Constants.GlobalSavesDirectoryName:
                            case Constants.LocalSavesDirectoryName:
                                string copyToPath = Path.Combine(serverData.TempSavesDirectoryPath, fi.Name);

                                var target = new FileInfo(copyToPath);
                                await fi.CopyToAsync(target);

                                fi = target;
                                break;
                            case Constants.TempSavesDirectoryName:
                                break;
                            default:
                                return Result.Failure(Constants.MissingFileErrorKey, $"File {saveFilePath} not found.");
                        }

                        string basePath = serverData.BaseDirectoryPath;

                        var startInfo = new ProcessStartInfo
                        {
#if WINDOWS
                            FileName = "C:/Program Files/dotnet/dotnet.exe",
                            Arguments = $"C:/Projects/FactorioWebInterface/FactorioWrapper/bin/Windows/netcoreapp2.1/FactorioWrapper.dll {serverId} {basePath}/bin/x64/factorio.exe --start-server {fi.FullName} --server-settings {basePath}/server-settings.json --port {serverData.Port}",
#elif WSL
                            FileName = "/usr/bin/dotnet",
                            Arguments = $"/mnt/c/Projects/FactorioWebInterface/FactorioWrapper/bin/Wsl/netcoreapp2.1/publish/FactorioWrapper.dll {serverId} {basePath}/bin/x64/factorio --start-server {fi.FullName} --server-settings {basePath}/server-settings.json --port {serverData.Port}",
#else
                            FileName = "/usr/bin/dotnet",
                            Arguments = $"/factorio/factorioWrapper/FactorioWrapper.dll {serverId} {basePath}/bin/x64/factorio --start-server {fi.FullName} --server-settings {basePath}/server-settings.json --port {serverData.Port}",
#endif
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        try
                        {
                            Process.Start(startInfo);
                        }
                        catch (Exception)
                        {
                            _logger.LogError("Error loading serverId: {serverId} file: {file}", serverId, filePath);
                            return Result.Failure(Constants.WrapperProcessErrorKey, "Wrapper process failed to start.");
                        }

                        _logger.LogInformation("Server load serverId: {serverId} file: {file} user: {userName}", serverId, filePath, userName);

                        serverData.Status = FactorioServerStatus.WrapperStarting;

                        var group = _factorioControlHub.Clients.Group(serverId);
                        await group.FactorioStatusChanged(FactorioServerStatus.WrapperStarting.ToString(), serverData.Status.ToString());

                        var message = new MessageData()
                        {
                            MessageType = MessageType.Control,
                            Message = $"Server load file: {fi.Name} by user: {userName}"
                        };

                        serverData.ControlMessageBuffer.Add(message);
                        await group.SendMessage(message);

                        return Result.OK;

                    default:
                        return Result.Failure(Constants.InvalidServerStateErrorKey, $"Cannot load server when in state {serverData.Status}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error loading", e);
                return Result.Failure(Constants.UnexpctedErrorKey);
            }
            finally
            {
                serverData.ServerLock.Release();
            }
        }

        public async Task<Result> Stop(string serverId, string userName)
        {
#if WINDOWS
            return Result.Failure(Constants.NotSupportedErrorKey, "Stop is not supported on Windows.");
#else
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return Result.Failure(Constants.ServerIdErrorKey, $"serverId {serverId} not found.");
            }

            switch (serverData.Status)
            {
                case FactorioServerStatus.Unknown:
                case FactorioServerStatus.WrapperStarted:
                case FactorioServerStatus.Starting:
                case FactorioServerStatus.Running:
                case FactorioServerStatus.Updated:
                    break;
                default:
                    return Result.Failure(Constants.InvalidServerStateErrorKey, $"Cannot stop server when in state {serverData.Status}");
            }

            var message = new MessageData()
            {
                MessageType = MessageType.Control,
                Message = $"Server stopped by user {userName}"
            };

            _ = SendToFactorioControl(serverId, message);
            await _factorioProcessHub.Clients.Groups(serverId).Stop();

            _logger.LogInformation("server stopped :serverId {serverId} user: {userName}", serverId, userName);

            return Result.OK;
#endif
        }

        public async Task<Result> ForceStop(string serverId, string userName)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return Result.Failure(Constants.ServerIdErrorKey, $"serverId {serverId} not found.");
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                switch (serverData.Status)
                {
                    case FactorioServerStatus.WrapperStarting:
                        _ = ChangeStatusNonLocking(serverData, FactorioServerStatus.Killed, userName);
                        break;
                    case FactorioServerStatus.Unknown:
                    case FactorioServerStatus.WrapperStarted:
                    case FactorioServerStatus.Starting:
                    case FactorioServerStatus.Running:
                    case FactorioServerStatus.Stopping:
                    case FactorioServerStatus.Killing:
                    case FactorioServerStatus.Updated:
                        var message = new MessageData()
                        {
                            MessageType = MessageType.Control,
                            Message = $"Server killed by user {userName}"
                        };

                        _ = SendControlMessageNonLocking(serverData, message);

                        break;
                    default:
                        return Result.Failure(Constants.InvalidServerStateErrorKey, $"Cannot force stop server when in state {serverData.Status}");
                }
            }
            finally
            {
                serverData.ServerLock.Release();
            }

            await _factorioProcessHub.Clients.Groups(serverId).ForceStop();

            _logger.LogInformation("server killed :serverId {serverId} user: {userName}", serverId, userName);

            return Result.OK;
        }

        public async Task<Result> Save(string serverId, string userName, string saveName)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return Result.Failure(Constants.ServerIdErrorKey, $"serverId {serverId} not found.");
            }

            if (serverData.Status != FactorioServerStatus.Running)
                return Result.Failure(Constants.InvalidServerStateErrorKey, $"Cannot save game when in state {serverData.Status}");

            var message = new MessageData()
            {
                MessageType = MessageType.Control,
                Message = $"Server saved by user {userName}"
            };
            _ = SendToFactorioControl(serverId, message);

            var command = FactorioCommandBuilder.SilentCommand()
                .Add("game.server_save(")
                .AddQuotedString(saveName)
                .Add(")")
                .Build();
            await SendToFactorioProcess(serverId, command);

            _logger.LogInformation("server saved :serverId {serverId} user: {userName}", serverId, userName);
            return Result.OK;
        }

        private async Task<Result> DownloadAndExtract(FactorioServerData serverData, string version)
        {
            try
            {
                string basePath = serverData.BaseDirectoryPath;
                var extractDirectoryPath = Path.Combine(basePath, "factorio");
                var binDirectoryPath = Path.Combine(basePath, "bin");
                var dataDirectoryPath = Path.Combine(basePath, "data");
                var binariesPath = Path.Combine(basePath, "binaries.tar.xz");

                var extractDirectory = new DirectoryInfo(extractDirectoryPath);
                if (extractDirectory.Exists)
                {
                    extractDirectory.Delete(true);
                }

                var client = _httpClientFactory.CreateClient();
                string url = $"https://factorio.com/get-download/{version}/headless/linux64";
                var download = await client.GetAsync(url);
                if (!download.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Update failed: Error downloading {url}", url);
                    return Result.Failure(Constants.UpdateErrorKey, "Error downloading file.");
                }

                var binaries = new FileInfo(binariesPath);

                using (var fs = binaries.OpenWrite())
                {
                    await download.Content.CopyToAsync(fs);
                }

                bool success = ExecuteProcess("/bin/tar", $"-xJf {binariesPath} -C {basePath}");

                var binDirectory = new DirectoryInfo(binDirectoryPath);
                if (binDirectory.Exists)
                {
                    binDirectory.Delete(true);
                }
                var dataDirectory = new DirectoryInfo(dataDirectoryPath);
                if (dataDirectory.Exists)
                {
                    dataDirectory.Delete(true);
                }

                if (success)
                {
                    Directory.Move(Path.Combine(extractDirectoryPath, "bin"), binDirectoryPath);
                    Directory.Move(Path.Combine(extractDirectoryPath, "data"), dataDirectoryPath);

                    var configFile = new FileInfo(Path.Combine(basePath, "config-path.cfg"));
                    if (!configFile.Exists)
                    {
                        var extractConfigFile = new FileInfo(Path.Combine(extractDirectoryPath, "config-path.cfg"));
                        if (extractConfigFile.Exists)
                        {
                            extractConfigFile.MoveTo(configFile.FullName);
                        }
                    }
                }

                extractDirectory.Refresh();
                if (extractDirectory.Exists)
                {
                    extractDirectory.Delete(true);
                }

                if (binaries.Exists)
                {
                    binaries.Delete();
                }

                if (success)
                {
                    return Result.OK;
                }
                else
                {
                    return Result.Failure("UpdateErrorKey", "Error extracting file.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(DownloadAndExtract));
                return Result.Failure(Constants.UnexpctedErrorKey, "Unexpected error installing.");
            }
        }

        /// SignalR processes one message at a time, so this method needs to return before the downloading starts.
        /// Else if the user clicks the update button twice in quick succession, the first request is finished before
        /// the second requests starts, meaning the update will happen twice.
        private void InstallInner(string serverId, FactorioServerData serverData, string version)
        {
            _ = Task.Run(async () =>
            {
                var result = await DownloadAndExtract(serverData, version);

                try
                {
                    await serverData.ServerLock.WaitAsync();

                    var oldStatus = serverData.Status;
                    var group = _factorioControlHub.Clients.Group(serverId);

                    if (result.Success)
                    {
                        serverData.Status = FactorioServerStatus.Updated;

                        await group.FactorioStatusChanged(FactorioServerStatus.Updated.ToString(), oldStatus.ToString());

                        var messageData = new MessageData()
                        {
                            MessageType = MessageType.Status,
                            Message = $"[STATUS]: Changed from {oldStatus} to {FactorioServerStatus.Updated}"
                        };

                        serverData.ControlMessageBuffer.Add(messageData);
                        await group.SendMessage(messageData);

                        _logger.LogInformation("Updated server.");
                    }
                    else
                    {
                        serverData.Status = FactorioServerStatus.Crashed;

                        await group.FactorioStatusChanged(FactorioServerStatus.Crashed.ToString(), oldStatus.ToString());

                        var messageData = new MessageData()
                        {
                            MessageType = MessageType.Status,
                            Message = $"[STATUS]: Changed from {oldStatus} to {FactorioServerStatus.Crashed}"
                        };

                        serverData.ControlMessageBuffer.Add(messageData);
                        await group.SendMessage(messageData);

                        var messageData2 = new MessageData()
                        {
                            MessageType = MessageType.Control,
                            Message = result.ToString()
                        };

                        serverData.ControlMessageBuffer.Add(messageData2);
                        await group.SendMessage(messageData2);
                    }

                }
                finally
                {
                    serverData.ServerLock.Release();
                }
            });
        }

        public async Task<Result> Install(string serverId, string userName, string version)
        {
#if WINDOWS
           return Result.Failure(Constants.NotSupportedErrorKey, "Install is not supported on windows.");
#else
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknow serverId: {serverId}", serverId);
                return Result.Failure($"Unknow serverId: {serverId}");
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                var oldStatus = serverData.Status;

                switch (oldStatus)
                {
                    case FactorioServerStatus.WrapperStarting:
                    case FactorioServerStatus.WrapperStarted:
                    case FactorioServerStatus.Starting:
                    case FactorioServerStatus.Running:
                    case FactorioServerStatus.Stopping:
                    case FactorioServerStatus.Killing:
                    case FactorioServerStatus.Updating:
                        return Result.Failure(Constants.InvalidServerStateErrorKey, $"Cannot Update server when in state {oldStatus}");
                    default:
                        break;
                }

                serverData.Status = FactorioServerStatus.Updating;

                var group = _factorioControlHub.Clients.Group(serverId);
                await group.FactorioStatusChanged(FactorioServerStatus.Updating.ToString(), oldStatus.ToString());

                var messageData = new MessageData()
                {
                    MessageType = MessageType.Status,
                    Message = $"[STATUS]: Changed from {oldStatus} to {FactorioServerStatus.Updating} by user {userName}"
                };

                serverData.ControlMessageBuffer.Add(messageData);
                await group.SendMessage(messageData);

                InstallInner(serverId, serverData, version);
            }
            finally
            {
                serverData.ServerLock.Release();
            }

            return Result.OK;
#endif
        }

        public async Task<FactorioServerStatus> GetStatus(string serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return FactorioServerStatus.Unknown;
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                return serverData.Status;
            }
            finally
            {
                serverData.ServerLock.Release();
            }
        }

        public Task RequestStatus(string serverId)
        {
            return _factorioProcessHub.Clients.Group(serverId).GetStatus();
        }

        public Task SendToFactorioProcess(string serverId, string data)
        {
            return _factorioProcessHub.Clients.Group(serverId).SendToFactorio(data);
        }

        public async Task SendToFactorioControl(string serverId, MessageData data)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknow serverId: {serverId}", serverId);
                return;
            }

            try
            {
                await serverData.ServerLock.WaitAsync();
                serverData.ControlMessageBuffer.Add(data);
            }
            finally
            {
                serverData.ServerLock.Release();
            }

            await _factorioControlHub.Clients.Group(serverId).SendMessage(data);
        }

        public async Task<MessageData[]> GetFactorioControlMessagesAsync(string serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknow serverId: {serverId}", serverId);
                return new MessageData[0];
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                var buffer = serverData.ControlMessageBuffer.TakeWhile(x => x != null).ToArray();
                return buffer;
            }
            finally
            {
                serverData.ServerLock.Release();
            }
        }

        public void FactorioDataReceived(string serverId, string data)
        {
            if (data == null)
            {
                return;
            }

            var messageData = new MessageData()
            {
                MessageType = MessageType.Output,
                Message = data
            };

            _ = SendToFactorioControl(serverId, messageData);

            var match = tag_regex.Match(data);
            if (!match.Success || match.Index > 20)
            {
                return;
            }

            var groups = match.Groups;
            string tag = groups[1].Value;
            string content = groups[2].Value;

            switch (tag)
            {
                case Constants.ChatTag:
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case Constants.DiscordTag:
                    content = content.Replace("\\n", "\n");
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case Constants.DiscordRawTag:
                    content = content.Replace("\\n", "\n");
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case Constants.DiscordBold:
                    content = content.Replace("\\n", "\n");
                    content = Formatter.Sanitize(content);
                    content = Formatter.Bold(content);
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case Constants.DiscordAdminTag:
                    content = content.Replace("\\n", "\n");
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioAdminChannel(content);
                    break;
                case Constants.DiscordAdminRawTag:
                    content = content.Replace("\\n", "\n");
                    _discordBot.SendToFactorioAdminChannel(content);
                    break;
                case Constants.JoinTag:
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, "**" + content + "**");
                    break;
                case Constants.LeaveTag:
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, "**" + content + "**");
                    break;
                case Constants.DiscordEmbedTag:
                    {
                        content = content.Replace("\\n", "\n");
                        content = Formatter.Sanitize(content);

                        var embed = new DiscordEmbedBuilder()
                        {
                            Description = content,
                            Color = DiscordBot.infoColor
                        };

                        _discordBot.SendEmbedToFactorioChannel(serverId, embed);
                        break;
                    }
                case Constants.DiscordEmbedRawTag:
                    {
                        content = content.Replace("\\n", "\n");

                        var embed = new DiscordEmbedBuilder()
                        {
                            Description = content,
                            Color = DiscordBot.infoColor
                        };

                        _discordBot.SendEmbedToFactorioChannel(serverId, embed);
                        break;
                    }

                case Constants.DiscordAdminEmbedTag:
                    {
                        content = content.Replace("\\n", "\n");
                        content = Formatter.Sanitize(content);

                        var embed = new DiscordEmbedBuilder()
                        {
                            Description = content,
                            Color = DiscordBot.infoColor
                        };

                        _discordBot.SendEmbedToFactorioAdminChannel(embed);
                        break;
                    }
                case Constants.DiscordAdminEmbedRawTag:
                    {
                        content = content.Replace("\\n", "\n");

                        var embed = new DiscordEmbedBuilder()
                        {
                            Description = content,
                            Color = DiscordBot.infoColor
                        };

                        _discordBot.SendEmbedToFactorioAdminChannel(embed);
                        break;
                    }
                case Constants.RegularPromoteTag:
                    _ = PromoteRegular(serverId, content);
                    break;
                case Constants.RegularDemoteTag:
                    _ = DemoteRegular(serverId, content);
                    break;
                default:
                    break;
            }
        }

        private async Task PromoteRegular(string serverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var parms = content.Trim().Split(' ');
            string target = parms[0];
            string promoter = parms.Length > 1 ? parms[1] : "<server>";

            var db = _dbContextFactory.Create();

            var regular = new Regular() { Name = target, Date = DateTimeOffset.Now, PromotedBy = promoter };

            db.Add(regular);
            await db.SaveChangesAsync();

            var command = FactorioCommandBuilder
                .ServerCommand("regular_promote")
                .AddQuotedString(target)
                .Build();

            foreach (var server in servers.Values)
            {
                var serverLock = server.ServerLock;

                try
                {
                    await serverLock.WaitAsync();

                    if (server.ServerId == serverId)
                    {
                        continue;
                    }

                    // todo what do if server is in starting status?
                    if (server.Status == FactorioServerStatus.Running)
                    {
                        _ = SendToFactorioProcess(server.ServerId, command);
                    }
                }
                finally
                {
                    serverLock.Release();
                }
            }
        }

        private async Task DemoteRegular(string serverId, string content)
        {
            content = content.Trim();

            var db = _dbContextFactory.Create();

            var regular = new Regular() { Name = content };

            db.Remove(regular);
            await db.SaveChangesAsync();

            var command = FactorioCommandBuilder
                .ServerCommand("regular_demote")
                .AddQuotedString(content)
                .Build();

            foreach (var server in servers.Values)
            {
                var serverLock = server.ServerLock;

                try
                {
                    await serverLock.WaitAsync();

                    if (server.ServerId == serverId)
                    {
                        continue;
                    }

                    // todo what do if server is in starting status?
                    if (server.Status == FactorioServerStatus.Running)
                    {
                        _ = SendToFactorioProcess(server.ServerId, command);
                    }
                }
                finally
                {
                    serverLock.Release();
                }
            }
        }

        public void FactorioWrapperDataReceived(string serverId, string data)
        {
            var messageData = new MessageData()
            {
                MessageType = MessageType.Wrapper,
                Message = data
            };

            _ = SendToFactorioControl(serverId, messageData);
        }

        private async Task ServerStarted(string serverId)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Description = "Server has started",
                Color = DiscordBot.successColor
            };
            var t1 = _discordBot.SendEmbedToFactorioChannel(serverId, embed);

            var regulars = await _dbContextFactory.Create().Regulars.Select(r => r.Name).ToArrayAsync();

            var cb = FactorioCommandBuilder.ServerCommand("regular_sync");
            cb.Add("{");
            foreach (var r in regulars)
            {
                cb.AddQuotedString(r);
                cb.Add(",");
            }
            cb.RemoveLast(1);
            cb.Add("}");

            await SendToFactorioProcess(serverId, cb.Build());
            await t1;
        }

        public async Task StatusChanged(string serverId, FactorioServerStatus newStatus, FactorioServerStatus oldStatus)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return;
            }

            Task discordTask = null;
            if (newStatus == oldStatus || oldStatus == FactorioServerStatus.Unknown)
            {
                // Do nothing.
            }
            else if (oldStatus == FactorioServerStatus.Starting && newStatus == FactorioServerStatus.Running)
            {
                discordTask = ServerStarted(serverId);
            }
            else if (oldStatus == FactorioServerStatus.Stopping && newStatus == FactorioServerStatus.Stopped
                || oldStatus == FactorioServerStatus.Killing && newStatus == FactorioServerStatus.Killed)
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = "Server has stopped",
                    Color = DiscordBot.infoColor
                };
                discordTask = _discordBot.SendEmbedToFactorioChannel(serverId, embed);
            }
            else if (newStatus == FactorioServerStatus.Crashed)
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = "Server has crashed",
                    Color = DiscordBot.failureColor
                };
                discordTask = _discordBot.SendEmbedToFactorioChannel(serverId, embed);
            }

            var groups = _factorioControlHub.Clients.Group(serverId);
            Task contorlTask1 = groups.FactorioStatusChanged(newStatus.ToString(), oldStatus.ToString());

            Task controlTask2 = null;
            if (newStatus != oldStatus)
            {
                var messageData = new MessageData()
                {
                    MessageType = MessageType.Status,
                    Message = $"[STATUS]: Changed from {oldStatus} to {newStatus}"
                };

                serverData.ControlMessageBuffer.Add(messageData);
                controlTask2 = groups.SendMessage(messageData);
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                var recordedOldStatus = serverData.Status;

                if (recordedOldStatus != newStatus)
                {
                    serverData.Status = newStatus;
                }

            }
            finally
            {
                serverData.ServerLock.Release();
            }

            if (discordTask != null)
                await discordTask;
            if (contorlTask1 != null)
                await contorlTask1;
            if (controlTask2 != null)
                await controlTask2;
        }

        public async Task<List<Regular>> GetRegularsAsync()
        {
            var db = _dbContextFactory.Create();
            return await db.Regulars.ToListAsync();
        }

        public async Task<List<Admin>> GetAdminsAsync()
        {
            var db = _dbContextFactory.Create();
            return await db.Admins.ToListAsync();
        }

        public async Task AddRegularsFromStringAsync(string data)
        {
            var db = _dbContextFactory.Create();
            var regulars = db.Regulars;

            var names = data.Split(',').Select(x => x.Trim());
            foreach (var name in names)
            {
                var regular = new Regular()
                {
                    Name = name,
                    Date = DateTimeOffset.Now,
                    PromotedBy = "<From old list>"
                };
                regulars.Add(regular);
            }

            await db.SaveChangesAsync();
        }

        public async Task AddAdminsFromStringAsync(string data)
        {
            var db = _dbContextFactory.Create();
            var admins = db.Admins;

            var names = data.Split(',').Select(x => x.Trim());
            foreach (var name in names)
            {
                var admin = new Admin()
                {
                    Name = name
                };
                admins.Add(admin);
            }

            await db.SaveChangesAsync();
        }

        public Task OnProcessRegistered(string serverId)
        {
            return _factorioProcessHub.Clients.Group(serverId).GetStatus();
        }

        private FileMetaData[] GetFilesMetaData(string path, string directory)
        {
            try
            {
                var di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    di.Create();
                }

                var files = di.EnumerateFiles("*.zip")
                    .Select(f => new FileMetaData()
                    {
                        Name = f.Name,
                        Directory = directory,
                        CreatedTime = f.CreationTimeUtc,
                        LastModifiedTime = f.LastWriteTimeUtc,
                        Size = f.Length
                    })
                    .ToArray();

                return files;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return new FileMetaData[0];
            }
        }

        public FileMetaData[] GetTempSaveFiles(string serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return new FileMetaData[0];
            }

            var path = serverData.TempSavesDirectoryPath;
            var dir = Path.Combine(serverId, Constants.TempSavesDirectoryName);

            return GetFilesMetaData(path, dir);
        }

        public FileMetaData[] GetLocalSaveFiles(string serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return new FileMetaData[0];
            }

            var path = serverData.LocalSavesDirectoroyPath;
            var dir = Path.Combine(serverId, Constants.LocalSavesDirectoryName);

            return GetFilesMetaData(path, dir);
        }

        public FileMetaData[] GetGlobalSaveFiles()
        {
            var path = FactorioServerData.GlobalSavesDirectoryPath;

            return GetFilesMetaData(path, Constants.GlobalSavesDirectoryName);
        }

        public FileInfo GetFile(string directory, string fileName)
        {
            string path = Path.Combine(FactorioServerData.baseDirectoryPath, directory, fileName);

            try
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    return fi;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(GetFile));
                return null;
            }
        }

        private bool IsSaveDirectory(string dirName)
        {
            switch (dirName)
            {
                case Constants.GlobalSavesDirectoryName:
                case Constants.LocalSavesDirectoryName:
                case Constants.TempSavesDirectoryName:
                    return true;
                default:
                    return false;
            }
        }

        private DirectoryInfo GetSaveDirectory(FactorioServerData serverData, string dirName)
        {
            switch (dirName)
            {
                case Constants.GlobalSavesDirectoryName:
                case Constants.PublicStartSavesDirectoryName:
                case Constants.PublicFinalSavesDirectoryName:
                case Constants.PublicOldSavesDirectoryName:
                    return new DirectoryInfo(Path.Combine(FactorioServerData.baseDirectoryPath, dirName));
                case Constants.LocalSavesDirectoryName:
                    return new DirectoryInfo(serverData.LocalSavesDirectoroyPath);
                case Constants.TempSavesDirectoryName:
                    return new DirectoryInfo(serverData.TempSavesDirectoryPath);
                default:
                    return null;
            }
        }

        public async Task<Result> UploadFiles(string directory, IList<IFormFile> files)
        {
            var errors = new List<Error>();

            foreach (var file in files)
            {
                string path = Path.Combine(FactorioServerData.baseDirectoryPath, directory, file.FileName);

                try
                {
                    var fi = new FileInfo(path);
                    if (!IsSaveDirectory(fi.Directory.Name))
                    {
                        errors.Add(new Error(Constants.FileErrorKey, $"{file.FileName}"));
                        continue;
                    }

                    if (fi.Exists)
                    {
                        errors.Add(new Error(Constants.FileAlreadyExistsErrorKey, $"{file.FileName} already exists."));
                        continue;
                    }


                    using (var writeStream = fi.OpenWrite())
                    using (var readStream = file.OpenReadStream())
                    {
                        await readStream.CopyToAsync(writeStream);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error Uploading file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error uploading {file.FileName}."));
                }
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public Result DeleteFiles(List<string> filePaths)
        {
            var errors = new List<Error>();

            foreach (string filePath in filePaths)
            {
                string path = Path.Combine(FactorioServerData.baseDirectoryPath, filePath);

                try
                {
                    var fi = new FileInfo(path);

                    if (!fi.Exists)
                    {
                        errors.Add(new Error(Constants.MissingFileErrorKey, $"{filePath} doesn't exists."));
                        continue;
                    }

                    if (!IsSaveDirectory(fi.Directory.Name))
                    {
                        errors.Add(new Error(Constants.FileErrorKey, $"Error deleting {filePath}."));
                        continue;
                    }

                    fi.Delete();
                }
                catch (Exception e)
                {
                    _logger.LogError("Error Deleting file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error deleting {filePath}."));
                }
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public Result MoveFiles(string destination, List<string> filePaths)
        {
            string dirPath = Path.Combine(FactorioServerData.baseDirectoryPath, destination);

            try
            {
                var di = new DirectoryInfo(dirPath);

                string dirName = di.Name;

                if (!IsSaveDirectory(dirName))
                {
                    return Result.Failure(Constants.FileErrorKey, $"Error moving files");
                }

                if (!di.Exists)
                {
                    di.Create();
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error moveing file.", e);
                return Result.Failure(Constants.FileErrorKey, $"Error moving files");
            }

            var errors = new List<Error>();

            foreach (var filePath in filePaths)
            {
                string path = Path.Combine(FactorioServerData.baseDirectoryPath, filePath);

                try
                {
                    var fi = new FileInfo(path);

                    if (!fi.Exists)
                    {
                        errors.Add(new Error(Constants.MissingFileErrorKey, $"{filePath} doesn't exists."));
                        continue;
                    }

                    if (!IsSaveDirectory(fi.Directory.Name))
                    {
                        errors.Add(new Error(Constants.FileErrorKey, $"Error moving {filePath}."));
                        continue;
                    }

                    string destinationFilePath = Path.Combine(dirPath, fi.Name);

                    var destinationFileInfo = new FileInfo(destinationFilePath);

                    if (destinationFileInfo.Exists)
                    {
                        errors.Add(new Error(Constants.FileAlreadyExistsErrorKey, $"{filePath} already exists."));
                        continue;
                    }

                    fi.MoveTo(destinationFilePath);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error moveing file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error moveing {filePath}."));
                }
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public async Task<Result> CopyFiles(string destination, List<string> filePaths)
        {
            string dirPath = Path.Combine(FactorioServerData.baseDirectoryPath, destination);

            try
            {
                var di = new DirectoryInfo(dirPath);

                string dirName = di.Name;

                if (!IsSaveDirectory(dirName))
                {
                    return Result.Failure(Constants.FileErrorKey, $"Error copying files");
                }

                if (!di.Exists)
                {
                    di.Create();
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error copying file.", e);
                return Result.Failure(Constants.FileErrorKey, $"Error copying files");
            }

            var errors = new List<Error>();

            foreach (var filePath in filePaths)
            {
                string path = Path.Combine(FactorioServerData.baseDirectoryPath, filePath);

                try
                {
                    var fi = new FileInfo(path);

                    if (!fi.Exists)
                    {
                        errors.Add(new Error(Constants.MissingFileErrorKey, $"{filePath} doesn't exists."));
                        continue;
                    }

                    if (!IsSaveDirectory(fi.Directory.Name))
                    {
                        errors.Add(new Error(Constants.FileErrorKey, $"Error copying {filePath}."));
                        continue;
                    }

                    string destinationFilePath = Path.Combine(dirPath, fi.Name);

                    var destinationFileInfo = new FileInfo(destinationFilePath);

                    if (destinationFileInfo.Exists)
                    {
                        errors.Add(new Error(Constants.FileAlreadyExistsErrorKey, $"{filePath} already exists."));
                        continue;
                    }

                    await fi.CopyToAsync(destinationFileInfo);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error moveing file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error moveing {filePath}."));
                }
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public Result RenameFile(string directoryPath, string fileName, string newFileName)
        {
            string dirPath = Path.Combine(FactorioServerData.baseDirectoryPath, directoryPath);

            try
            {
                var di = new DirectoryInfo(dirPath);

                string dirName = di.Name;

                if (!IsSaveDirectory(dirName))
                {
                    return Result.Failure(Constants.FileErrorKey, $"Error renaming files");
                }

                string filePath = Path.Combine(dirPath, fileName);
                var fi = new FileInfo(filePath);

                if (!fi.Exists)
                {
                    return Result.Failure(Constants.MissingFileErrorKey, $"File {fileName} doesn't exist.");
                }

                string newFilePath = Path.Combine(dirPath, newFileName);
                var newFileInfo = new FileInfo(newFilePath);

                if (newFileInfo.Exists)
                {
                    return Result.Failure(Constants.FileAlreadyExistsErrorKey, $"File {fileName} already exists.");
                }

                if (newFileInfo.Extension != ".zip")
                {
                    newFilePath += ".zip";
                }

                fi.MoveTo(newFilePath);

                return Result.OK;
            }
            catch (Exception e)
            {
                _logger.LogError("Error renaming file.", e);
                return Result.Failure(Constants.FileErrorKey, $"Error renaming files");
            }
        }

        private async Task<FactorioServerSettings> GetServerSettings(FactorioServerData serverData)
        {
            var serverSettings = serverData.ServerSettings;

            if (serverSettings != null)
            {
                return serverSettings;
            }

            var fi = new FileInfo(serverData.ServerSettingsPath);

            if (!fi.Exists)
            {
                serverSettings = FactorioServerSettings.MakeDefault(_configuration);

                var a = await GetAdminsAsync();
                serverSettings.Admins = a.Select(x => x.Name).ToList();

                serverData.ServerSettings = serverSettings;

                var data = JsonConvert.SerializeObject(serverSettings, Formatting.Indented);
                using (var fs = fi.CreateText())
                {
                    await fs.WriteAsync(data);
                }

                return serverSettings;
            }
            else
            {
                using (var s = fi.OpenText())
                {
                    string output = await s.ReadToEndAsync();
                    serverSettings = JsonConvert.DeserializeObject<FactorioServerSettings>(output);
                }

                serverData.ServerSettings = serverSettings;

                return serverSettings;
            }
        }

        public async Task<FactorioServerSettingsWebEditable> GetEditableServerSettings(string serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return null;
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                var serverSettigns = await GetServerSettings(serverData);

                if (serverSettigns == null)
                {
                    return new FactorioServerSettingsWebEditable();
                }

                var editableSettings = new FactorioServerSettingsWebEditable()
                {
                    Name = serverSettigns.Name,
                    Description = serverSettigns.Description,
                    Tags = serverSettigns.Tags,
                    MaxPlayers = serverSettigns.MaxPlayers,
                    GamePassword = serverSettigns.GamePassword,
                    AutoPause = serverSettigns.AutoPause,
                    Admins = serverSettigns.Admins
                };

                return editableSettings;
            }
            finally
            {
                serverData.ServerLock.Release();
            }
        }


        public async Task<Result> SaveEditableServerSettings(string serverId, FactorioServerSettingsWebEditable settings)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknown serverId: {serverId}", serverId);
                return null;
            }

            try
            {
                await serverData.ServerLock.WaitAsync();

                var serverSettigns = await GetServerSettings(serverData);

                serverSettigns.Name = settings.Name;
                serverSettigns.Description = settings.Description;
                serverSettigns.Tags = settings.Tags;
                serverSettigns.MaxPlayers = settings.MaxPlayers < 0 ? 0 : settings.MaxPlayers;
                serverSettigns.GamePassword = settings.GamePassword;
                serverSettigns.AutoPause = settings.AutoPause;

                List<string> admins;

                int count = settings.Admins.Count(x => !string.IsNullOrWhiteSpace(x));

                if (count != 0)
                {
                    admins = settings.Admins.Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                }
                else
                {
                    var a = await GetAdminsAsync();
                    admins = a.Select(x => x.Name).ToList();
                }

                serverSettigns.Admins = admins;

                var data = JsonConvert.SerializeObject(serverSettigns, Formatting.Indented);

                await File.WriteAllTextAsync(serverData.ServerSettingsPath, data);

                return Result.OK;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception saving server settings.");
                return Result.Failure(Constants.UnexpctedErrorKey);
            }
            finally
            {
                serverData.ServerLock.Release();
            }
        }
    }
}
