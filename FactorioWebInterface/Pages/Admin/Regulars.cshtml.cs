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
    public class RegularsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IFactorioServerManager _factorioServerManager;

        public RegularsModel(UserManager<ApplicationUser> userManger, IFactorioServerManager factorioServerManager)
        {
            _userManger = userManger;
            _factorioServerManager = factorioServerManager;
        }

        public List<Regular> Regulars { get; private set; }

        public class InputModel
        {
            public string Regulars { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "Regulars");
                return RedirectToPage("SignIn");
            }

            Regulars = await _factorioServerManager.GetRegularsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var data = Input.Regulars;
            await _factorioServerManager.AddRegularsFromStringAsync(data);

            return RedirectToPage();
        }
    }
}