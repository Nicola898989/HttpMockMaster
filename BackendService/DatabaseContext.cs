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
                .IsRequired();

            modelBuilder.Entity<Models.HttpRequest>()
                .Property(r => r.Headers)
                .HasColumnType("TEXT");

            modelBuilder.Entity<Models.HttpRequest>()
                .Property(r => r.Body)
                .HasColumnType("TEXT");

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
        }
    }
}
