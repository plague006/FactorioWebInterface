using DSharpPlus.Entities;
using FactorioWebInterface.Hubs;
using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServerManager : IFactorioServerManager
    {
        private static readonly int serverCount = 4;

        private readonly IDiscordBot _discordBot;

        private IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> _factorioProcessHub;
        private IHubContext<FactorioControlHub, IFactorioControlClientMethods> _factorioControlHub;

        private DiscordChannel channel;

        private Dictionary<int, FactorioServerData> servers = FactorioServerData.Servers;

        public FactorioServerManager
        (
            IDiscordBot discordBot,
            IHubContext<FactorioProcessHub, IFactorioProcessClientMethods> factorioProcessHub,
            IHubContext<FactorioControlHub, IFactorioControlClientMethods> factorioControlHub
        )
        {
            _discordBot = discordBot;
            _factorioProcessHub = factorioProcessHub;
            _factorioControlHub = factorioControlHub;

            Init().GetAwaiter().GetResult();
        }

        public async Task Init()
        {
            channel = await _discordBot.DiscordClient.GetChannelAsync(487652968221376531);

            _discordBot.DiscordClient.MessageCreated += async e =>
            {
                if (e.Author.IsBot)
                {
                    return;
                }

                if (e.Message.ChannelId == 487652968221376531)
                {
                    await _factorioProcessHub.Clients.Group("1").SendToFactorio(e.Message.Content);
                    await _factorioControlHub.Clients.Group("1").FactorioOutputData(e.Message.Content);
                }
            };
        }

        public bool Start(int serverId)
        {
            if (!servers.TryGetValue(serverId, out var serverData))
            {
                return false;
            }

            string basePath = serverData.BaseDirectoryPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/dotnet",
                Arguments = $"factorio/factorioWrapper/FactorioWrapper.dll {serverId} {basePath}bin/x64/factorio --start-server-load-latest --server-settings {basePath}server-settings.json",
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
                return false;
            }

            return true;
        }

        public bool Load(int serverId, string saveFilePath)
        {
            throw new System.NotImplementedException();
        }

        public void Stop(int serverId)
        {
            _factorioProcessHub.Clients.Groups(serverId.ToString()).Stop();
        }

        public void ForceStop(int serverId)
        {
            _factorioProcessHub.Clients.Groups(serverId.ToString()).ForceStop();
        }

        public FactorioServerStatus GetStatus(int serverId)
        {
            throw new System.NotImplementedException();
        }

        public void SendToFactorio(int serverId, string data)
        {
            // todo send to factorio
            //_factorioProcessHub.Clients.Group(serverId.ToString()).SendToFactorio(data);

            channel.SendMessageAsync(data);
        }

        public void FactorioDataReceived(int serverId, string data)
        {
            int index = data.IndexOf("[CHAT]");
            if (index >= 0)
            {
                var message = data.Substring(index);
                channel.SendMessageAsync(message);
            }

            _factorioControlHub.Clients.Groups(serverId.ToString()).FactorioOutputData(data);
        }

        public void FactorioWrapperDataReceived(int serverId, string data)
        {
            _factorioControlHub.Clients.Groups(serverId.ToString()).FactorioWrapperOutputData(data);
        }
    }
}
