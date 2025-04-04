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
                            var isPostgres = Environment.GetEnvironmentVariable("DATABASE_URL") != null;
                            
                            if (isPostgres)
                            {
                                // PostgreSQL schema creation
                                cmd.CommandText = @"
                                    CREATE TABLE IF NOT EXISTS ""Requests"" (
                                        ""Id"" SERIAL PRIMARY KEY,
                                        ""Url"" TEXT NOT NULL,
                                        ""Method"" TEXT NOT NULL,
                                        ""Headers"" TEXT,
                                        ""Body"" TEXT,
                                        ""Timestamp"" TEXT NOT NULL,
                                        ""IsProxied"" BOOLEAN NOT NULL,
                                        ""TargetDomain"" TEXT
                                    );
                                    
                                    CREATE TABLE IF NOT EXISTS ""Responses"" (
                                        ""Id"" SERIAL PRIMARY KEY,
                                        ""RequestId"" INTEGER,
                                        ""StatusCode"" INTEGER NOT NULL,
                                        ""Headers"" TEXT,
                                        ""Body"" TEXT,
                                        ""Timestamp"" TEXT NOT NULL
                                    );
                                    
                                    CREATE TABLE IF NOT EXISTS ""Rules"" (
                                        ""Id"" SERIAL PRIMARY KEY,
                                        ""Name"" TEXT NOT NULL,
                                        ""Description"" TEXT,
                                        ""Method"" TEXT,
                                        ""PathPattern"" TEXT,
                                        ""QueryPattern"" TEXT,
                                        ""HeaderPattern"" TEXT,
                                        ""BodyPattern"" TEXT,
                                        ""Priority"" INTEGER NOT NULL,
                                        ""IsActive"" BOOLEAN NOT NULL,
                                        ""ResponseId"" INTEGER NOT NULL,
                                        FOREIGN KEY (""ResponseId"") REFERENCES ""Responses""(""Id"") ON DELETE CASCADE
                                    );
                                    
                                    CREATE TABLE IF NOT EXISTS ""TestScenarios"" (
                                        ""Id"" SERIAL PRIMARY KEY,
                                        ""Name"" TEXT NOT NULL,
                                        ""Description"" TEXT,
                                        ""CreatedAt"" TEXT NOT NULL,
                                        ""LastRunAt"" TEXT,
                                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                                    );
                                    
                                    CREATE TABLE IF NOT EXISTS ""ScenarioSteps"" (
                                        ""Id"" SERIAL PRIMARY KEY,
                                        ""TestScenarioId"" INTEGER NOT NULL,
                                        ""HttpRequestId"" INTEGER,
                                        ""HttpResponseId"" INTEGER,
                                        ""Name"" TEXT NOT NULL,
                                        ""Description"" TEXT,
                                        ""Order"" INTEGER NOT NULL,
                                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                                        FOREIGN KEY (""TestScenarioId"") REFERENCES ""TestScenarios""(""Id"") ON DELETE CASCADE,
                                        FOREIGN KEY (""HttpRequestId"") REFERENCES ""Requests""(""Id"") ON DELETE SET NULL,
                                        FOREIGN KEY (""HttpResponseId"") REFERENCES ""Responses""(""Id"") ON DELETE SET NULL
                                    );";
                            }
                            else
                            {
                                // SQLite schema creation
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
                                    );
                                    
                                    CREATE TABLE IF NOT EXISTS TestScenarios (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        Name TEXT NOT NULL,
                                        Description TEXT,
                                        CreatedAt TEXT NOT NULL,
                                        LastRunAt TEXT,
                                        IsActive INTEGER NOT NULL DEFAULT 1
                                    );
                                    
                                    CREATE TABLE IF NOT EXISTS ScenarioSteps (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        TestScenarioId INTEGER NOT NULL,
                                        HttpRequestId INTEGER,
                                        HttpResponseId INTEGER,
                                        Name TEXT NOT NULL,
                                        Description TEXT,
                                        Order INTEGER NOT NULL,
                                        IsActive INTEGER NOT NULL DEFAULT 1,
                                        FOREIGN KEY (TestScenarioId) REFERENCES TestScenarios(Id) ON DELETE CASCADE,
                                        FOREIGN KEY (HttpRequestId) REFERENCES Requests(Id) ON DELETE SET NULL,
                                        FOREIGN KEY (HttpResponseId) REFERENCES Responses(Id) ON DELETE SET NULL
                                    );";
                            }
                            
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
