using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public class InputModel
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string Reason { get; set; }
            [Required]
            public string Admin { get; set; }
            [Required]
            [Display(Name ="Date Time")]
            public DateTime DateTime { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "bans");
                return RedirectToPage("signIn");
            }

            Input = new InputModel
            {
                Admin = user.UserName,
                DateTime = DateTime.UtcNow
            };

            Bans = await _factorioServerManager.GetBansAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddBanAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "bans");
                return RedirectToPage("signIn");
            }

            if (!ModelState.IsValid)
            {
                Bans = await _factorioServerManager.GetBansAsync();
                return Page();
            }

            Ban ban = new Ban()
            {
                Username = Input.Username,
                Reason = Input.Reason,
                Admin = Input.Admin,
                DateTime = Input.DateTime
            };

            await _factorioServerManager.BanPlayer(ban);
            Bans = await _factorioServerManager.GetBansAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveBanAsync(string username, string reason)
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "bans");
                return RedirectToPage("signIn");
            }

            await _factorioServerManager.UnBanPlayer(username, user.UserName);
            Bans = await _factorioServerManager.GetBansAsync();

            Input = new InputModel
            {
                Username = username,
                Reason = reason,
                Admin = user.UserName,
                DateTime = DateTime.UtcNow
            };
            ModelState.Clear();

            return Page();
        }
    }
}