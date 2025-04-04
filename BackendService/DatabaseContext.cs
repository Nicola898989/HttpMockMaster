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
        public DbSet<ResponseTemplate> ResponseTemplates { get; set; } = null!;

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

            // Use text columns for JSON data, but ensure compatibility with PostgreSQL
            var dbProvider = Database.ProviderName;
            if (dbProvider != null && dbProvider.Contains("Sqlite"))
            {
                // SQLite configuration
                modelBuilder.Entity<Models.HttpRequest>()
                    .Property(r => r.Headers)
                    .HasColumnType("TEXT");

                modelBuilder.Entity<Models.HttpRequest>()
                    .Property(r => r.Body)
                    .HasColumnType("TEXT");
            }
            else
            {
                // PostgreSQL configuration
                modelBuilder.Entity<Models.HttpRequest>()
                    .Property(r => r.Headers)
                    .HasColumnType("text");

                modelBuilder.Entity<Models.HttpRequest>()
                    .Property(r => r.Body)
                    .HasColumnType("text");
            }
                
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
                
            // Configura la relazione tra HttpRequest e HttpResponse
            modelBuilder.Entity<Models.HttpResponse>()
                .HasOne(r => r.Request)
                .WithOne(r => r.Response)
                .HasForeignKey<Models.HttpResponse>(r => r.RequestId);

            if (dbProvider != null && dbProvider.Contains("Sqlite"))
            {
                // SQLite configuration for HttpResponse
                modelBuilder.Entity<Models.HttpResponse>()
                    .Property(r => r.Headers)
                    .HasColumnType("TEXT");

                modelBuilder.Entity<Models.HttpResponse>()
                    .Property(r => r.Body)
                    .HasColumnType("TEXT");
            }
            else
            {
                // PostgreSQL configuration for HttpResponse
                modelBuilder.Entity<Models.HttpResponse>()
                    .Property(r => r.Headers)
                    .HasColumnType("text");

                modelBuilder.Entity<Models.HttpResponse>()
                    .Property(r => r.Body)
                    .HasColumnType("text");
            }

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
                
            // Configure ResponseTemplate
            modelBuilder.Entity<ResponseTemplate>()
                .HasKey(rt => rt.Id);
                
            modelBuilder.Entity<ResponseTemplate>()
                .Property(rt => rt.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            modelBuilder.Entity<ResponseTemplate>()
                .HasIndex(rt => rt.Name)
                .IsUnique();
                
            modelBuilder.Entity<ResponseTemplate>()
                .Property(rt => rt.IsSystem)
                .IsRequired()
                .HasDefaultValue(false);
                
            modelBuilder.Entity<ResponseTemplate>()
                .Property(rt => rt.CreatedAt)
                .IsRequired();
                
            if (dbProvider != null && dbProvider.Contains("Sqlite"))
            {
                // SQLite configuration for ResponseTemplate
                modelBuilder.Entity<ResponseTemplate>()
                    .Property(rt => rt.Body)
                    .HasColumnType("TEXT");
                    
                modelBuilder.Entity<ResponseTemplate>()
                    .Property(rt => rt.Headers)
                    .HasColumnType("TEXT");
                    
                modelBuilder.Entity<ResponseTemplate>()
                    .Property(rt => rt.Description)
                    .HasColumnType("TEXT");
            }
            else
            {
                // PostgreSQL configuration for ResponseTemplate
                modelBuilder.Entity<ResponseTemplate>()
                    .Property(rt => rt.Body)
                    .HasColumnType("text");
                    
                modelBuilder.Entity<ResponseTemplate>()
                    .Property(rt => rt.Headers)
                    .HasColumnType("text");
                    
                modelBuilder.Entity<ResponseTemplate>()
                    .Property(rt => rt.Description)
                    .HasColumnType("text");
            }
        }
    }
}
