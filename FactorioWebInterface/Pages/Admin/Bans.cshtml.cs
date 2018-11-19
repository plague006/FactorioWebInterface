using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Pages.Admin
{
    public class BansModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IFactorioServerManager _factorioServerManager;

        public BansModel(UserManager<ApplicationUser> userManger, IFactorioServerManager factorioServerManager)
        {
            _userManger = userManger;
            _factorioServerManager = factorioServerManager;
        }

        public List<Ban> Bans { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "bans");
                return RedirectToPage("signIn");
            }

            Bans = await _factorioServerManager.GetBansAsync();

            return Page();
        }
    }
}