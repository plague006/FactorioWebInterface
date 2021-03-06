﻿using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace FactorioWebInterface.Models
{
    public class DiscordBot
    {
        public static readonly DiscordColor infoColor = new DiscordColor(0, 127, 255);
        public static readonly DiscordColor successColor = DiscordColor.Green;
        public static readonly DiscordColor failureColor = DiscordColor.Red;

        public DiscordBot(DiscordBotContext discordBotContext, IFactorioServerManager factorioServerManager)
        {
            var d = new DependencyCollectionBuilder()
                .AddInstance(discordBotContext)
                .AddInstance(factorioServerManager)
                .Build();

            var commands = discordBotContext.DiscordClient.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = ";;",
                Dependencies = d,
                CaseSensitive = false
            });

            commands.RegisterCommands<DiscordBotCommands>();
        }
    }
}
