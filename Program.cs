using System.Text.Json.Serialization;
using BackendService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTP server to listen on port 8000
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8000);
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Database context
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MockAMMT",
    "interceptor.db");

// Ensure directory exists
Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register HTTP services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HttpClient>();

// Register application services
builder.Services.AddScoped<RuleService>();
builder.Services.AddScoped<ProxyService>();
builder.Services.AddSingleton<InterceptorService>();
builder.Services.AddHostedService<InterceptorService>(sp => sp.GetRequiredService<InterceptorService>());

// Set up logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure middleware pipeline
app.UseCors("AllowAll");
app.UseRouting();
app.MapControllers();

// Ensure database is created/migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
