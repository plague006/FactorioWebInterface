using DSharpPlus;
using DSharpPlus.Entities;
using FactorioWebInterface.Hubs;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServerManager : IFactorioServerManager
    {
        private static readonly Regex tag_regex = new Regex(@"(\[[^\[\]]+\])\s*((?:.|\s)*)\s*", RegexOptions.Compiled);

        private readonly IDiscordBot _discordBot;
        private IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> _factorioProcessHub;
        private IHubContext<FactorioControlHub, IFactorioControlClientMethods> _factorioControlHub;
        private readonly ILogger<FactorioServerManager> _logger;

        private SemaphoreSlim serverLock = new SemaphoreSlim(1, 1);
        private Dictionary<string, FactorioServerData> servers = FactorioServerData.Servers;

        public FactorioServerManager
        (
            IDiscordBot discordBot,
            IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> factorioProcessHub,
            IHubContext<FactorioControlHub, IFactorioControlClientMethods> factorioControlHub,
            ILogger<FactorioServerManager> logger
        )
        {
            _discordBot = discordBot;
            _factorioProcessHub = factorioProcessHub;
            _factorioControlHub = factorioControlHub;
            _logger = logger;

            _discordBot.FactorioDiscordDataReceived += FactorioDiscordDataReceived;
        }

        private void FactorioDiscordDataReceived(IDiscordBot sender, ServerMessageEventArgs eventArgs)
        {
            string data = $"/silent-command game.print('[Discord] {eventArgs.User.Username}: {eventArgs.Message}')";
            SendToFactorioProcess(eventArgs.ServerId, data);
            SendToFactorioControl(eventArgs.ServerId, $"[Discord] {eventArgs.User.Username}: {eventArgs.Message}");
        }

        public bool Start(string serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                _logger.LogError("Unknow serverId: {serverId}", serverId);
                return false;
            }

            string basePath = serverData.BaseDirectoryPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/dotnet",
                Arguments = $"/factorio/factorioWrapper/FactorioWrapper.dll {serverId} {basePath}bin/x64/factorio --start-server-load-latest --server-settings {basePath}server-settings.json --port {serverData.Port}",
                //FileName = "C:/Program Files/dotnet/dotnet.exe",
                //Arguments = $"C:/Projects/FactorioWebInterface/FactorioWrapper/bin/Release/netcoreapp2.1/publish/FactorioWrapper.dll {serverId} C:/factorio/Factorio1/bin/x64/factorio.exe --start-server C:/factorio/Factorio1/bin/x64/test.zip --server-settings C:/factorio/Factorio1/bin/x64/server-settings.json",

                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception)
            {
                _logger.LogError("Error starting serverId: {serverId}", serverId);
                return false;
            }

            _logger.LogInformation("Server started serverId: {serverId}", serverId);
            return true;
        }

        public bool Load(string serverId, string saveFilePath)
        {
            throw new System.NotImplementedException();
        }

        public void Stop(string serverId)
        {
            _factorioProcessHub.Clients.Groups(serverId).Stop();
        }

        public void ForceStop(string serverId)
        {
            _factorioProcessHub.Clients.Groups(serverId).ForceStop();
        }

        public async Task<FactorioServerStatus> GetStatus(string serverId)
        {
            try
            {
                await serverLock.WaitAsync();

                if (!servers.TryGetValue(serverId, out var serverData))
                {
                    _logger.LogError("Unknow serverId: {serverId}", serverId);
                    //todo throw error?
                    return FactorioServerStatus.Unknown;
                }

                return serverData.Status;
            }
            finally
            {
                serverLock.Release();
            }
        }

        public void SendToFactorioProcess(string serverId, string data)
        {
            _factorioProcessHub.Clients.Group(serverId).SendToFactorio(data);
        }

        public void SendToFactorioControl(string serverId, string data)
        {
            _factorioControlHub.Clients.Group(serverId).FactorioOutputData(data);
        }

        public void FactorioDataReceived(string serverId, string data)
        {
            SendToFactorioControl(serverId, data);

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
                case "[CHAT]":
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case "[DISCORD]":
                    content = content.Replace("\\n", "\n");
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case "[DISCORD-RAW]":
                    content = content.Replace("\\n", "\n");
                    _discordBot.SendToFactorioChannel(serverId, content);
                    break;
                case "[DISCORD-ADMIN]":
                    content = content.Replace("\\n", "\n");
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioAdminChannel(content);
                    break;
                case "[DISCORD-ADMIN-RAW]":
                    content = content.Replace("\\n", "\n");
                    _discordBot.SendToFactorioAdminChannel(content);
                    break;
                case "[JOIN]":
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, "**" + content + "**");
                    break;
                case "[LEAVE]":
                    content = Formatter.Sanitize(content);
                    _discordBot.SendToFactorioChannel(serverId, "**" + content + "**");
                    break;
                case "[DISCORD-EMBED]":
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
                case "[DISCORD-EMBED-RAW]":
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

                case "[DISCORD-ADMIN-EMBED]":
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
                case "[DISCORD-ADMIN-EMBED-RAW]":
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
                default:
                    break;
            }
        }

        public void FactorioWrapperDataReceived(string serverId, string data)
        {
            SendToFactorioControl(serverId, data);
        }

        public async Task StatusChanged(string serverId, FactorioServerStatus newStatus, FactorioServerStatus oldStatus)
        {
            try
            {
                await serverLock.WaitAsync();

                if (!servers.TryGetValue(serverId, out var serverData))
                {
                    _logger.LogError("Unknow serverId: {serverId}", serverId);
                }

                serverData.Status = newStatus;

            }
            finally
            {
                serverLock.Release();
            }

            await _factorioControlHub.Clients.Group(serverId).FactorioStatusChanged(newStatus.ToString(), oldStatus.ToString());
        }
    }
}
