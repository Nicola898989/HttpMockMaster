using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Enable listening on port 8888 for non-admin users (Windows)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    var proc = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = "http add urlacl url=http://+:8888/ user=Everyone",
                            Verb = "runas",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        }
                    };
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to run netsh command: {ex.Message}");
                    Console.WriteLine("You may need to run the application as administrator or manually set HTTP permissions.");
                }
            }

            var host = CreateHostBuilder(args).Build();
            
            // Apply migrations on startup
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                db.Database.Migrate();
            }
            
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Database
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "HttpInterceptor",
                        "interceptor.db");
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

                    services.AddDbContext<DatabaseContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));

                    // HTTP Client for proxy
                    services.AddHttpClient();
                    services.AddSingleton<HttpClient>();
                    
                    // Services
                    services.AddScoped<RuleService>();
                    services.AddScoped<ProxyService>();
                    services.AddHostedService<InterceptorService>();

                    // Controllers
                    services.AddControllers();
                    
                    // CORS
                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowAll", builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}
