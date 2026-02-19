using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KitchenPrint.ENTITIES
{
    /// <summary>
    /// Design-time factory for KitchenPrint DbContext.
    /// Allows EF Core tools (Add-Migration, Update-Database) to create
    /// the DbContext without running Program.cs and its runtime dependencies (JWT, etc.).
    /// Reads connection string from: environment variables > user-secrets > appsettings.json
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<kitchenPrintDbContext>
    {
        public kitchenPrintDbContext CreateDbContext(string[] args)
        {
            // Build configuration from the API project's directory
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "KitchenPrint-backend");
            var resolvedBasePath = Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory();

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(resolvedBasePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            // Load user-secrets from the API startup project (same UserSecretsId)
            var apiProjectPath = Path.Combine(resolvedBasePath, "KitchenPrint-backend.csproj");
            if (File.Exists(apiProjectPath))
            {
                configBuilder.AddUserSecrets("337dc540-b1c6-42be-bce4-a5b6d0b266ac");
            }

            var configuration = configBuilder.Build();

            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "Connection string 'PostgreSQL' not found. " +
                    "Set it via: dotnet user-secrets set \"ConnectionStrings:PostgreSQL\" \"Host=...;Database=...;Username=...;Password=...\" " +
                    "or environment variable ConnectionStrings__PostgreSQL");

            var optionsBuilder = new DbContextOptionsBuilder<kitchenPrintDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new kitchenPrintDbContext(optionsBuilder.Options);
        }
    }
}
