using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FactorioWebInterface.Data;
using FactorioWebInterface.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class DiscordBot : IDiscordBot
    {
        public static readonly DiscordColor infoColor = new DiscordColor(0, 127, 255);
        public static readonly DiscordColor successColor = DiscordColor.Green;
        public static readonly DiscordColor failureColor = DiscordColor.Red;

        private readonly IConfiguration _configuration;
        private readonly DbContextFactory _dbContextFactory;
        private readonly ulong guildId;
        private readonly ulong adminRoleId;

        private readonly SemaphoreSlim discordLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<ulong, string> discordToServer = new Dictionary<ulong, string>();
        private readonly Dictionary<string, ulong> serverdToDiscord = new Dictionary<string, ulong>();

        public DiscordClient DiscordClient { get; private set; }

        public event EventHandler<IDiscordBot, ServerMessageEventArgs> FactorioDiscordDataReceived;

        public DiscordBot(IConfiguration configuration, DbContextFactory dbContextFactory)
        {
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;
            guildId = ulong.Parse(_configuration[Constants.GuildIDKey]);
            adminRoleId = ulong.Parse(_configuration[Constants.AdminRoleIDKey]);

            InitAsync().GetAwaiter().GetResult();
        }

        private async Task InitAsync()
        {
            DiscordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = _configuration[Constants.DiscordBotTokenKey],
                TokenType = TokenType.Bot
            });

            var d = new DependencyCollectionBuilder().AddInstance(this).Build();

            DiscordClient.MessageCreated += DiscordClient_MessageCreated;

            var commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = ";;",
                Dependencies = d,
                CaseSensitive = false
            });

            commands.RegisterCommands<DiscordBotCommands>();

            var discordTask = DiscordClient.ConnectAsync();

            using (var context = _dbContextFactory.Create())
            {
                foreach (var ds in context.DiscordServers)
                {
                    discordToServer[ds.DiscordChannelId] = ds.ServerId;
                    serverdToDiscord[ds.ServerId] = ds.DiscordChannelId;
                }
            }

            await discordTask;
        }

        private async Task DiscordClient_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsCurrent)
            {
                return;
            }

            string serverId;
            try
            {
                await discordLock.WaitAsync();

                if (!discordToServer.TryGetValue(e.Channel.Id, out serverId))
                {
                    return;
                }
            }
            finally
            {
                discordLock.Release();
            }

            FactorioDiscordDataReceived?.Invoke(this, new ServerMessageEventArgs(serverId, e.Author, e.Message.Content));
        }

        /// <summary>
        /// Returns a boolean for if the discord user has the admin role in the Redmew guild.
        /// </summary>
        /// <param name="userId">The discord user's id.</param>        
        public async Task<bool> IsAdminRoleAsync(ulong userId)
        {
            var guild = await DiscordClient.GetGuildAsync(guildId);
            var memeber = await guild.GetMemberAsync(userId);

            if (memeber == null)
                return false;

            var role = memeber.Roles.FirstOrDefault(x => x.Id == adminRoleId);

            return role != null;
        }

        /// <summary>
        /// Returns a boolean for if the discord user has the admin role in the Redmew guild.
        /// </summary>
        /// <param name="userId">The discord user's id.</param> 
        public Task<bool> IsAdminRoleAsync(string userId)
        {
            if (ulong.TryParse(userId, out ulong id))
                return IsAdminRoleAsync(id);
            else
                return Task.FromResult(false);
        }

        public async Task<bool> SetServer(string serverId, ulong channelId)
        {
            //todo check serverId is valid.
            try
            {
                await discordLock.WaitAsync();

                using (var context = _dbContextFactory.Create())
                {
                    var query = context.DiscordServers.Where(x => x.DiscordChannelId == channelId || x.ServerId == serverId);

                    foreach (var ds in query)
                    {
                        serverdToDiscord.Remove(ds.ServerId);
                        discordToServer.Remove(ds.DiscordChannelId);
                        context.DiscordServers.Remove(ds);
                    }

                    serverdToDiscord.Add(serverId, channelId);
                    discordToServer.Add(channelId, serverId);

                    context.Add(new DiscordServers() { DiscordChannelId = channelId, ServerId = serverId });

                    await context.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                discordLock.Release();
            }
        }

        public async Task<string> UnSetServer(ulong channelId)
        {
            try
            {
                await discordLock.WaitAsync();

                using (var context = _dbContextFactory.Create())
                {
                    var query = context.DiscordServers.Where(x => x.DiscordChannelId == channelId);

                    string serverId = null;

                    foreach (var ds in query)
                    {
                        serverdToDiscord.Remove(ds.ServerId);
                        discordToServer.Remove(ds.DiscordChannelId);
                        context.DiscordServers.Remove(ds);

                        serverId = ds.ServerId;
                    }

                    await context.SaveChangesAsync();

                    return serverId;
                }
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                discordLock.Release();
            }
        }

        public async Task SendToFactorioChannel(string serverId, string data)
        {
            ulong channelId;
            try
            {
                await discordLock.WaitAsync();
                if (!serverdToDiscord.TryGetValue(serverId, out channelId))
                {
                    return;
                }
            }
            finally
            {
                discordLock.Release();
            }

            var channel = await DiscordClient.GetChannelAsync(channelId);
            await channel.SendMessageAsync(data);
        }

        public async Task SendEmbedToFactorioChannel(string serverId, DiscordEmbed embed)
        {
            ulong channelId;
            try
            {
                await discordLock.WaitAsync();
                if (!serverdToDiscord.TryGetValue(serverId, out channelId))
                {
                    return;
                }
            }
            finally
            {
                discordLock.Release();
            }

            var channel = await DiscordClient.GetChannelAsync(channelId);

            await channel.SendMessageAsync(embed: embed);
        }

        public Task SendToFactorioAdminChannel(string data)
        {
            return SendToFactorioChannel(Constants.AdminChannelID, data);
        }

        public Task SendEmbedToFactorioAdminChannel(DiscordEmbed embed)
        {
            return SendEmbedToFactorioChannel(Constants.AdminChannelID, embed);
        }
    }
}
