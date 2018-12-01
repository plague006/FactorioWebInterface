using Newtonsoft.Json;
using System.ComponentModel;

namespace FactorioWebInterface.Models
{
    public class FactorioServerExtraSettings
    {
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool SyncBans { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool BuildBansFromDatabaseOnStart { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool SetDiscordChannelName { get; set; }

        public static FactorioServerExtraSettings Default()
        {
            return new FactorioServerExtraSettings()
            {
                SyncBans = true,
                BuildBansFromDatabaseOnStart = true,
                SetDiscordChannelName = true
            };
        }

        public FactorioServerExtraSettings Copy()
        {
            return new FactorioServerExtraSettings()
            {
                SyncBans = SyncBans,
                BuildBansFromDatabaseOnStart = BuildBansFromDatabaseOnStart,
                SetDiscordChannelName = SetDiscordChannelName
            };
        }
    }
}
