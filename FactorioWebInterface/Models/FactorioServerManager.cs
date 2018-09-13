using System.Collections.Generic;

namespace FactorioWebInterface.Models
{
    public class FactorioServerManager : IFactorioServerManager
    {
        private static readonly int serverCount = 1;

        private IDiscordBot _discordBot;

        private List<FactorioServer> servers;

        public FactorioServerManager(IDiscordBot discordBot)
        {
            _discordBot = discordBot;

            servers = new List<FactorioServer>();
            for (int i = 0; i < serverCount; i++)
            {
                var s = new FactorioServer(i + 1);
                s.Init(discordBot).GetAwaiter().GetResult();
                servers.Add(s);
            }
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
