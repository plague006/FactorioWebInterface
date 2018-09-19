using FactorioWebInterface.Hubs;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FactorioWebInterface.Models
{
    public class FactorioServerManager : IFactorioServerManager
    {
        private readonly IDiscordBot _discordBot;

        private IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> _factorioProcessHub;
        private IHubContext<FactorioControlHub, IFactorioControlClientMethods> _factorioControlHub;
        private readonly ILogger<FactorioServerManager> _logger;

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
            SendToFactorio(eventArgs.ServerId, eventArgs.Data);
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

        public FactorioServerStatus GetStatus(string serverId)
        {
            throw new System.NotImplementedException();
        }

        public void SendToFactorio(string serverId, string data)
        {
            _factorioProcessHub.Clients.Group(serverId).SendToFactorio(data);
            _factorioControlHub.Clients.Group(serverId).FactorioOutputData(data);
        }

        public void FactorioDataReceived(string serverId, string data)
        {
            int index = data.IndexOf("[CHAT]");
            if (index >= 0)
            {
                var message = data.Substring(index);

                _discordBot.SendToFactorioChannel(serverId, message);
            }

            _factorioControlHub.Clients.Groups(serverId.ToString()).FactorioOutputData(data);
        }

        public void FactorioWrapperDataReceived(string serverId, string data)
        {
            _factorioControlHub.Clients.Groups(serverId.ToString()).FactorioWrapperOutputData(data);
        }
    }
}
