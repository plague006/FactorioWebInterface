using System.ComponentModel;

namespace FactorioWebInterface.Models
{
    public class FactorioServerExtraSettings
    {
        [DefaultValue(true)]
        public bool SyncBans { get; set; }

        [DefaultValue(true)]
        public bool BuildBansFromDatabaseOnStart { get; set; }
    }
}
