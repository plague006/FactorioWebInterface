using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServerManager : IFactorioServerManager
    {
        private static readonly int serverCount = 1;

        private readonly IDiscordBot _discordBot;
        private readonly IFactorioProcessRelay _factorioProcessRelay;

        private DiscordChannel channel;

        private List<FactorioServer> servers;

        public FactorioServerManager(IDiscordBot discordBot, IFactorioProcessRelay factorioProcessRelay)
        {
            _discordBot = discordBot;
            _factorioProcessRelay = factorioProcessRelay;

            Init(discordBot, _factorioProcessRelay).GetAwaiter().GetResult();

            //servers = new List<FactorioServer>();
            //for (int i = 0; i < serverCount; i++)
            //{
            //    var s = new FactorioServer(i + 1);
            //    s.Init(discordBot).GetAwaiter().GetResult();
            //    servers.Add(s);
            //}
        }

        public async Task Init(IDiscordBot bot, IFactorioProcessRelay relay)
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
                    await relay.SendToFactorio("1", e.Message.Content);
                }
            };

            relay.FactorioDataReceived += (s, e) =>
            {
                int index = e.Data.IndexOf("[CHAT]");
                if (index >= 0)
                {
                    var message = e.Data.Substring(index);
                    channel.SendMessageAsync(message);
                }
            };
        }

        public void StartWrapper(int serverId)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/dotnet",
                Arguments = $"/factorio/factorio/factorioWrapper/FactorioWrapper.dll {serverId} /factorio/factorio/bin/x64/factorio --start-server-load-latest --server-settings /factorio/factorio/server-settings.json",
                //FileName = "C:/Program Files/dotnet/dotnet.exe",
                //Arguments = $"C:/Projects/FactorioWebInterface/FactorioWrapper/bin/Release/netcoreapp2.1/publish/FactorioWrapper.dll {serverId} C:/factorio/Factorio1/bin/x64/factorio.exe --start-server C:/factorio/Factorio1/bin/x64/test.zip --server-settings C:/factorio/Factorio1/bin/x64/server-settings.json",

                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }

        public FactorioServer GetServer(int serverId)
        {
            if (serverId < 1 || serverId > servers.Count)
            {
                return null;
            }

            return servers[serverId - 1];
        }
    }
}
