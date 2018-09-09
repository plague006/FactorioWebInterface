using FactorioWebInterface.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace FactorioWebInterface.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;

        public IndexModel(UserManager<ApplicationUser> userManger)
        {
            _userManger = userManger;
        }

        public int ID { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null)
            {
                HttpContext.Session.SetString("returnUrl", "Index/2");
                return RedirectToPage("Login");
            }

            ID = id ?? 1;

            return Page();
        }
    }
}