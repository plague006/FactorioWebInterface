using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioProcessRelayEventArgs
    {
        public FactorioProcessRelayEventArgs(string serverId, string data)
        {
            ServerId = serverId;
            Data = data;
        }

        public string ServerId { get; }
        public string Data { get; }
    }
}
