using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BackendService;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackendServiceTests.TestHelpers
{
    public static class DatabaseHelper
    {
        public static DatabaseContext CreateInMemoryDatabase(string dbName = null)
        {
            // Generare un nome random per il database se non specificato
            dbName = dbName ?? Guid.NewGuid().ToString();
            
            // Configurare un'istanza di database in memoria
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            
            var context = new DatabaseContext(options);
            
            // Assicurarsi che il database sia creato
            context.Database.EnsureCreated();
            
            return context;
        }
        
        public static ILogger<T> CreateLogger<T>()
        {
            return new NullLogger<T>();
        }
    }
}