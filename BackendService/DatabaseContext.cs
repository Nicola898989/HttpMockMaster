using System;
using Microsoft.EntityFrameworkCore;
using BackendService.Models;

namespace BackendService
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Models.HttpRequest> Requests { get; set; } = null!;
        public DbSet<Models.HttpResponse> Responses { get; set; } = null!;
        public DbSet<Rule> Rules { get; set; } = null!;
        public DbSet<TestScenario> TestScenarios { get; set; } = null!;
        public DbSet<ScenarioStep> ScenarioSteps { get; set; } = null!;

        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure HttpRequest
            modelBuilder.Entity<Models.HttpRequest>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Models.HttpRequest>()
                .Property(r => r.Url)
                .IsRequired();

            modelBuilder.Entity<Models.HttpRequest>()
                .Property(r => r.Method)
                .IsRequired()
                .HasMaxLength(10);  // GET, POST, PUT, etc.

            modelBuilder.Entity<Models.HttpRequest>()
                .Property(r => r.Headers)
                .HasColumnType("TEXT");

            modelBuilder.Entity<Models.HttpRequest>()
                .Property(r => r.Body)
                .HasColumnType("TEXT");
                
            // Add indexes for better query performance
            modelBuilder.Entity<Models.HttpRequest>()
                .HasIndex(r => r.Timestamp);
                
            modelBuilder.Entity<Models.HttpRequest>()
                .HasIndex(r => r.Method);
                
            modelBuilder.Entity<Models.HttpRequest>()
                .HasIndex(r => r.IsProxied);

            // Configure HttpResponse
            modelBuilder.Entity<Models.HttpResponse>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Models.HttpResponse>()
                .Property(r => r.StatusCode)
                .IsRequired();

            modelBuilder.Entity<Models.HttpResponse>()
                .Property(r => r.Headers)
                .HasColumnType("TEXT");

            modelBuilder.Entity<Models.HttpResponse>()
                .Property(r => r.Body)
                .HasColumnType("TEXT");

            // Configure Rule
            modelBuilder.Entity<Rule>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<Rule>()
                .Property(r => r.Name)
                .IsRequired();

            modelBuilder.Entity<Rule>()
                .HasOne(r => r.Response)
                .WithMany()
                .HasForeignKey(r => r.ResponseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure TestScenario
            modelBuilder.Entity<TestScenario>()
                .HasKey(ts => ts.Id);

            modelBuilder.Entity<TestScenario>()
                .Property(ts => ts.Name)
                .IsRequired();
                
            modelBuilder.Entity<TestScenario>()
                .Property(ts => ts.CreatedAt)
                .IsRequired();
                
            // Configure ScenarioStep
            modelBuilder.Entity<ScenarioStep>()
                .HasKey(ss => ss.Id);
                
            modelBuilder.Entity<ScenarioStep>()
                .HasOne(ss => ss.TestScenario)
                .WithMany(ts => ts.Steps)
                .HasForeignKey(ss => ss.TestScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ScenarioStep>()
                .HasOne(ss => ss.HttpRequest)
                .WithMany()
                .HasForeignKey(ss => ss.HttpRequestId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<ScenarioStep>()
                .HasOne(ss => ss.HttpResponse)
                .WithMany()
                .HasForeignKey(ss => ss.HttpResponseId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
