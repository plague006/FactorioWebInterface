using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Pages
{
    public class BansModel : PageModel
    {
        private readonly IFactorioServerManager _factorioServerManager;

        public BansModel(IFactorioServerManager factorioServerManager)
        {
            _factorioServerManager = factorioServerManager;
        }

        public List<string> Bans { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Bans = await _factorioServerManager.GetBanUserNamesAsync();

            return Page();
        }
    }
}