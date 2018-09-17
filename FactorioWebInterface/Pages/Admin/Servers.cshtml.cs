using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace FactorioWebInterface.Pages.Admin
{
    public class ServersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IFactorioServerManager _factorioServerManager;

        public ServersModel(UserManager<ApplicationUser> userManger, IFactorioServerManager factorioServerManager)
        {
            _userManger = userManger;
            _factorioServerManager = factorioServerManager;
        }

        public class InputModel
        {
            public int Id { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public int ID { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ID = id ?? 1;

            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "Servers/" + ID);
                return RedirectToPage("SignIn");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "Servers/" + 1);
                return RedirectToPage("SignIn");
            }

            if (Input.Id != 0)
            {
                _factorioServerManager.Start(Input.Id);
            }

            return Page();
        }
    }
}