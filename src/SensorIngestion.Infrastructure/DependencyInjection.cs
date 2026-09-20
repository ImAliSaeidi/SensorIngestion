using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SensorIngestion.Application.Ingestion;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Infrastructure.JsonLines;
using SensorIngestion.Infrastructure.Persistence;

namespace SensorIngestion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredPath = configuration["Persistence:DatabasePath"];
        var databasePath = string.IsNullOrWhiteSpace(configuredPath) ? "data/sensor-ingestion.db" : configuredPath;
        var resolvedPath = Path.IsPathRooted(databasePath) ? Path.GetFullPath(databasePath) : Path.GetFullPath(databasePath, AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);

        services.AddDbContext<SensorIngestionDbContext>(options => options.UseSqlite($"Data Source={resolvedPath}"));
        services.AddScoped<IRuleCatalog, EfRuleCatalog>();
        services.AddScoped<IIngestionPersistence, EfIngestionPersistence>();
        services.AddSingleton<IReadingParser, JsonlReadingParser>();
        services.AddSingleton<IReadingSource>(_ => new JsonlFileReadingSource(ResolveInputPath(configuration)));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IngestionProcessor>();
        return services;
    }

    private static string ResolveInputPath(IConfiguration configuration)
    {
        var configuredPath = configuration["Input:Path"];
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("Input file path is not configured.");

        return Path.IsPathRooted(configuredPath) ? Path.GetFullPath(configuredPath) : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
    }
}
