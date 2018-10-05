using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FactorioWebInterface.Pages.Admin
{
    public class ServersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IFactorioServerManager _factorioServerManager;
        private readonly ILogger<ServersModel> _logger;

        public ServersModel(UserManager<ApplicationUser> userManger, IFactorioServerManager factorioServerManager, ILogger<ServersModel> logger)
        {
            _userManger = userManger;
            _factorioServerManager = factorioServerManager;
            _logger = logger;
        }

        public class InputModel
        {
            public string Id { get; set; }
        }

        //[BindProperty]
        //public InputModel Input { get; set; }

        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            Id = id ?? 1;

            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "Servers/" + Id);
                return RedirectToPage("SignIn");
            }

            return Page();
        }

        public async Task<IActionResult> OnGetFile(string directory, string name)
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "Servers/" + Id);
                return RedirectToPage("SignIn");
            }

            var file = _factorioServerManager.GetFile(directory, name);
            if (file == null)
            {
                return BadRequest();
            }

            return File(file.OpenRead(), "application/zip", file.Name);
        }
    }
}