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
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Npgsql;

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
            
            // Configurazione del database più leggera per l'ambiente di Replit
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                Console.WriteLine("Initializing database...");
                
                try 
                {
                    // Utilizziamo solo EnsureCreated per evitare la complessità delle migrazioni
                    // Questo è più veloce e adatto per ambienti di sviluppo/test
                    if (db.Database.EnsureCreated())
                    {
                        Console.WriteLine("Database creato con successo");
                    }
                    else
                    {
                        Console.WriteLine("Database già esistente, utilizzo schema corrente");
                    }
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine($"Errore durante l'inizializzazione del database: {ex.Message}");
                    
                    // In caso di errore forniamo informazioni più dettagliate ma non tentiamo 
                    // operazioni pesanti di fallback che potrebbero causare timeout
                    var dbProvider = db.Database.ProviderName;
                    var connectionString = db.Database.GetConnectionString();
                    
                    Console.WriteLine($"Provider database: {dbProvider ?? "Sconosciuto"}");
                    if (connectionString != null)
                    {
                        // Mostra parte della connessione per diagnostica senza esporre credenziali
                        var sanitizedConnectionString = connectionString.Contains(";") 
                            ? string.Join(";", connectionString.Split(';').Take(2).Append("..."))
                            : "[Mascherato per sicurezza]";
                        
                        Console.WriteLine($"Connessione (parziale): {sanitizedConnectionString}");
                    }
                }
            }
            
            // Configure the HTTP request pipeline
            app.UseRouting();
            app.UseCors("AllowAll");
            
            // Add caching middleware
            app.UseResponseCaching();
            
            // Add cache control headers to API responses
            app.Use(async (context, next) =>
            {
                // Add cache control headers for API routes only
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    // Dynamic content like /api/requests shouldn't be cached by proxies
                    // but can still use the ResponseCache attribute
                    context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10),
                        MustRevalidate = true
                    };
                }
                
                await next();
            });
            
            app.MapControllers();
            
            // Start the application
            app.Run("http://0.0.0.0:5000");
        }
        
        private static void ConfigureServices(IServiceCollection services)
        {
            // Database configuration - supporting both SQLite and PostgreSQL
            var postgresConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
            
            if (!string.IsNullOrEmpty(postgresConnectionString))
            {
                // Using PostgreSQL when DATABASE_URL is provided
                Console.WriteLine("Using PostgreSQL database");
                
                // Parse the connection string properly if it's in the format:
                // postgresql://username:password@host:port/database?param=value
                if (postgresConnectionString.StartsWith("postgresql://"))
                {
                    try
                    {
                        // Split the connection string to get the query parameters
                        string connectionPart = postgresConnectionString;
                        string queryParams = "";
                        
                        if (postgresConnectionString.Contains("?"))
                        {
                            var parts = postgresConnectionString.Split(new[] { '?' }, 2);
                            connectionPart = parts[0];
                            queryParams = parts[1];
                        }
                        
                        var uri = new Uri(connectionPart);
                        var userInfo = uri.UserInfo.Split(':');
                        var host = uri.Host;
                        var port = uri.Port > 0 ? uri.Port : 5432;
                        var database = uri.AbsolutePath.TrimStart('/');
                        var username = userInfo[0];
                        var password = userInfo.Length > 1 ? userInfo[1] : "";
                        
                        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
                        {
                            Host = host,
                            Port = port,
                            Database = database,
                            Username = username,
                            Password = password,
                            SslMode = Npgsql.SslMode.Require // Default to require SSL for cloud databases
                        };
                        
                        // Parse query parameters
                        if (!string.IsNullOrEmpty(queryParams))
                        {
                            foreach (var param in queryParams.Split('&'))
                            {
                                if (param.Contains("="))
                                {
                                    var keyValue = param.Split(new[] { '=' }, 2);
                                    var key = keyValue[0];
                                    var value = keyValue[1];
                                    
                                    if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (Enum.TryParse<Npgsql.SslMode>(value, true, out var sslMode))
                                        {
                                            npgsqlBuilder.SslMode = sslMode;
                                        }
                                    }
                                    // Add other parameter parsing as needed
                                }
                            }
                        }
                        
                        // Ensure Trust Server Certificate is set to true
                        // Npgsql 6.0+ requires this to be explicitly set for SSL connections
                        postgresConnectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                        Console.WriteLine($"Parsed connection string: Host={host}, Database={database}, Username={username}, SSL=Require with Trust Server Certificate");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing PostgreSQL connection string: {ex.Message}");
                        // Fallback to direct usage of the connection string
                        var connStr = postgresConnectionString.Replace("postgresql://", "");
                        
                        // Direttamente costruisci una stringa di connessione valida per Npgsql
                        var parts = connStr.Split('@');
                        if (parts.Length == 2)
                        {
                            var userPass = parts[0].Split(':');
                            var hostDbParts = parts[1].Split('/');
                            
                            if (userPass.Length >= 2 && hostDbParts.Length >= 2)
                            {
                                var username = userPass[0];
                                var password = userPass[1];
                                var hostPort = hostDbParts[0].Split(':');
                                var host = hostPort[0];
                                var port = hostPort.Length > 1 ? hostPort[1] : "5432";
                                var dbAndParams = hostDbParts[1].Split('?');
                                var db = dbAndParams[0];
                                
                                postgresConnectionString = $"Host={host};Port={port};Database={db};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
                                Console.WriteLine($"Manual connection string parsing: Host={host}, Database={db}, Username={username}");
                            }
                        }
                    }
                }
                
                Console.WriteLine($"Using final connection string format: {(postgresConnectionString.StartsWith("Host=") ? "Npgsql format" : "Original format")}");
                services.AddDbContext<DatabaseContext>(options =>
                    options.UseNpgsql(postgresConnectionString));
            }
            else
            {
                // Fallback to SQLite when PostgreSQL is not configured
                var dbPath = "interceptor.db";
                Console.WriteLine($"Using SQLite database path: {Path.GetFullPath(dbPath)}");
                services.AddDbContext<DatabaseContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
            }
                
            // Add memory cache for improved performance
            services.AddMemoryCache();
                
            // HTTP Client for proxy with resilience
            services.AddHttpClient("ProxyClient", options => {
                options.Timeout = TimeSpan.FromSeconds(30);
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Keep connections alive longer
            services.AddSingleton<HttpClient>();
            
            // Services
            services.AddScoped<RuleService>();
            services.AddSingleton<NetworkSimulationService>();
            services.AddScoped<ProxyService>();
            services.AddScoped<TestScenarioService>();
            services.AddScoped<ExportService>();
            services.AddSingleton<InterceptorService>();
            services.AddHostedService(provider => provider.GetRequiredService<InterceptorService>());
            
            // Controllers and API with response caching
            services.AddControllers();
            services.AddResponseCaching(options => {
                options.MaximumBodySize = 64 * 1024 * 1024; // 64MB
                options.UseCaseSensitivePaths = false;
            });
            
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
