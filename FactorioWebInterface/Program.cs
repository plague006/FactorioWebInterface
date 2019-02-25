using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Shared.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FactorioWebInterface
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.Combine(path, "logs/log.txt");

            Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.Async(a => a.File(path, rollingInterval: RollingInterval.Day))
           .CreateLogger();

            try
            {
                Log.Information("Starting factorio web interface");

                var host = CreateWebHostBuilder(args).Build();

                // This makes sure the databases are setup.
                SeedData(host);

                // This makes sure the FactorioServerManger is started when the web interface starts.
                host.Services.GetService<IFactorioServerManager>();

                host.Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(o => o.Limits.MaxRequestBodySize = 209715200) // 200MB.
                .UseSerilog();

        private static void SeedData(IWebHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var appDb = services.GetService<ApplicationDbContext>();
                appDb.Database.Migrate();

                var scenarioDb = services.GetService<ScenarioDbContext>();
                scenarioDb.Database.Migrate();

                var roleManager = services.GetService<RoleManager<IdentityRole>>();

                roleManager.CreateAsync(new IdentityRole(Constants.RootRole));
                roleManager.CreateAsync(new IdentityRole(Constants.AdminRole));
            }
        }
    }
}
