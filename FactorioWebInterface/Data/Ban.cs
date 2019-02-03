using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace FactorioWebInterface.Data
{
    public class Ban
    {
        [Key]
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "reason")]
        public string Reason { get; set; }

        public string Address { get; set; }

        [JsonProperty(PropertyName = "admin")]
        public string Admin { get; set; }

        [JsonProperty(PropertyName = "dateTime")]
        public DateTime DateTime { get; set; }
    }
}
