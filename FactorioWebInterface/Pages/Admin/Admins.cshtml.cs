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
    public class AdminsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IFactorioServerManager _factorioServerManager;

        public AdminsModel(UserManager<ApplicationUser> userManger, IFactorioServerManager factorioServerManager)
        {
            _userManger = userManger;
            _factorioServerManager = factorioServerManager;
        }

        public List<Data.Admin> Admins { get; private set; }

        public class InputModel
        {
            public string Admins { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "admins");
                return RedirectToPage("signIn");
            }

            Admins = await _factorioServerManager.GetAdminsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var data = Input.Admins;
            await _factorioServerManager.AddAdminsFromStringAsync(data);

            return RedirectToPage();
        }
    }
}