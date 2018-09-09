using Microsoft.AspNetCore.Identity;

namespace FactorioWebInterface.Data
{
    public class ApplicationUser : IdentityUser
    {
        public bool Suspended { get; set; }
    }
}
