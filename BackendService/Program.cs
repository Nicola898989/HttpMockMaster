using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

            var builder = WebApplication.CreateBuilder(args);
            
            // Add services to the container
            ConfigureServices(builder.Services);
            
            var app = builder.Build();
            
            // Apply migrations and create database on startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                Console.WriteLine("Ensuring database is created...");
                db.Database.EnsureCreated();
                Console.WriteLine("Applying migrations...");
                try 
                {
                    db.Database.Migrate();
                    Console.WriteLine("Migrations applied successfully");
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine($"Error applying migrations: {ex.Message}");
                    Console.WriteLine("Attempting to create tables manually...");
                    
                    // Manual table creation if migrations fail
                    try
                    {
                        var conn = db.Database.GetDbConnection();
                        if (conn.State != System.Data.ConnectionState.Open)
                            conn.Open();
                        
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                CREATE TABLE IF NOT EXISTS Requests (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Url TEXT NOT NULL,
                                    Method TEXT NOT NULL,
                                    Headers TEXT,
                                    Body TEXT,
                                    Timestamp TEXT NOT NULL,
                                    IsProxied INTEGER NOT NULL,
                                    TargetDomain TEXT
                                );
                                
                                CREATE TABLE IF NOT EXISTS Responses (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    RequestId INTEGER,
                                    StatusCode INTEGER NOT NULL,
                                    Headers TEXT,
                                    Body TEXT,
                                    Timestamp TEXT NOT NULL
                                );
                                
                                CREATE TABLE IF NOT EXISTS Rules (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Name TEXT NOT NULL,
                                    Description TEXT,
                                    Method TEXT,
                                    PathPattern TEXT,
                                    QueryPattern TEXT,
                                    HeaderPattern TEXT,
                                    BodyPattern TEXT,
                                    Priority INTEGER NOT NULL,
                                    IsActive INTEGER NOT NULL,
                                    ResponseId INTEGER NOT NULL,
                                    FOREIGN KEY (ResponseId) REFERENCES Responses(Id) ON DELETE CASCADE
                                );";
                            cmd.ExecuteNonQuery();
                        }
                        Console.WriteLine("Tables created manually successfully");
                    }
                    catch (Exception manualEx)
                    {
                        Console.WriteLine($"Failed to create tables manually: {manualEx.Message}");
                    }
                }
            }
            
            // Configure the HTTP request pipeline
            app.UseRouting();
            app.UseCors("AllowAll");
            
            app.MapControllers();
            
            // Start the application
            app.Run("http://0.0.0.0:5000");
        }
        
        private static void ConfigureServices(IServiceCollection services)
        {
            // Database
            var dbPath = "interceptor.db";
            
            // Output path for debugging
            Console.WriteLine($"Using database path: {Path.GetFullPath(dbPath)}");
            
            services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
                
            // HTTP Client for proxy
            services.AddHttpClient();
            services.AddSingleton<HttpClient>();
            
            // Services
            services.AddScoped<RuleService>();
            services.AddScoped<ProxyService>();
            services.AddHostedService<InterceptorService>();
            
            // Controllers and API
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
        }
    }
}
