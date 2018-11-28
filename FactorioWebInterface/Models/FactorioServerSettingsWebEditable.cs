using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServerSettingsWebEditable
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; }

        [JsonProperty(PropertyName = "max_players")]
        public int MaxPlayers { get; set; }    

        [JsonProperty(PropertyName = "game_password")]
        public string GamePassword { get; set; }

        [JsonProperty(PropertyName = "auto_pause")]
        public bool AutoPause { get; set; }

        [JsonProperty(PropertyName = "admins")]
        public List<string> Admins { get; set; }

        [JsonProperty(PropertyName = "autosave_interval")]
        public int AutosaveInterval { get; set; }

        [JsonProperty(PropertyName = "autosave_slots")]
        public int AutosaveSlots { get; set; }

        [JsonProperty(PropertyName = "public_visible")]
        public bool PublicVisible { get; set; }
    }
}
