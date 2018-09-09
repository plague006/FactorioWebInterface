using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FactorioWebInterface
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            //SeedData(host);

            //Bot.StartAsync().GetAwaiter().GetResult();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        private static void SeedData(IWebHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetService<RoleManager<IdentityRole>>();

                roleManager.CreateAsync(new IdentityRole(Constants.RootRole));
                roleManager.CreateAsync(new IdentityRole(Constants.AdminRole));
            }
        }
    }
}
