using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql;

namespace Quay27.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c>. Loads the same connection string as the API when possible:
/// <c>QUAY27_CONNECTION_STRING</c> overrides; otherwise <c>Quay27-Be/appsettings*.json</c>.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var env = Environment.GetEnvironmentVariable("QUAY27_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        var apiDir = FindDirectoryContainingFile("Quay27-Be.csproj");
        if (apiDir != null)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var config = new ConfigurationBuilder()
                .SetBasePath(apiDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(cs))
                return cs;
        }

        throw new InvalidOperationException(
            "EF design-time: set environment variable QUAY27_CONNECTION_STRING, " +
            "or add ConnectionStrings:DefaultConnection to Quay27-Be/appsettings.json " +
            "and run `dotnet ef` from the solution folder (so Quay27-Be.csproj can be found).");
    }

    private static string? FindDirectoryContainingFile(string fileName)
    {
        foreach (var root in GetSearchRoots())
        {
            try
            {
                var dir = new DirectoryInfo(root);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, fileName)))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignore invalid paths
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        yield return Directory.GetCurrentDirectory();
        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir))
            yield return baseDir;
    }
}
