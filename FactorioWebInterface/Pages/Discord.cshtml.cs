using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FactorioWebInterface.Pages
{
    public class DiscordModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Redirect("https://discordapp.com/invite/HuzMbde");
        }
    }
}