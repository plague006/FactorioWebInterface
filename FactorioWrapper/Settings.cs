using Newtonsoft.Json;

namespace FactorioWrapper
{
    public class Settings
    {
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }
}
