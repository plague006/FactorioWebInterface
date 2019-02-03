using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    [Authorize]
    public class FactorioBanHub : Hub<IFactorioBanClientMethods>
    {
        private IFactorioServerManager _factorioServerManager;

        public FactorioBanHub(IFactorioServerManager factorioServerManager)
        {
            _factorioServerManager = factorioServerManager;
        }

        public Task<List<Ban>> GetAllBans()
        {
            return _factorioServerManager.GetBansAsync();
        }

        public Task<Result> AddBan(Ban ban, bool synchronizeWithServers)
        {
            return _factorioServerManager.BanPlayer(ban, synchronizeWithServers);
        }

        public Task<Result> RemoveBan(string username, bool synchronizeWithServers)
        {
            string admin = Context.User.Identity.Name;
            return _factorioServerManager.UnBanPlayer(username, admin, synchronizeWithServers);
        }
    }
}
