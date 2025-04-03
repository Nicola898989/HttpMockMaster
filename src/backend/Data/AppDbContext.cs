using Microsoft.EntityFrameworkCore;
using System;

public class AppDbContext : DbContext
{
    public DbSet<HttpRequest> Requests { get; set; }
    public DbSet<HttpResponse> Responses { get; set; }
    public DbSet<Rule> Rules { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure HttpRequest entity
        modelBuilder.Entity<HttpRequest>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<HttpRequest>()
            .HasMany(r => r.Responses)
            .WithOne(r => r.Request)
            .HasForeignKey(r => r.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure HttpResponse entity
        modelBuilder.Entity<HttpResponse>()
            .HasKey(r => r.Id);

        // Configure Rule entity
        modelBuilder.Entity<Rule>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<Rule>()
            .HasMany(r => r.Responses)
            .WithOne(r => r.Rule)
            .HasForeignKey(r => r.RuleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed initial data - create a default "welcome" rule
        modelBuilder.Entity<Rule>().HasData(
            new Rule
            {
                Id = 1,
                Name = "Welcome Response",
                PathPattern = "/welcome",
                StatusCode = 200,
                ContentType = "application/json",
                Headers = "{\"X-Powered-By\": \"HTTP Interceptor\"}",
                ResponseBody = "{\"message\": \"Welcome to HTTP Interceptor\", \"status\": \"running\"}",
                IsActive = true,
                Created = DateTime.UtcNow
            }
        );
    }
}
