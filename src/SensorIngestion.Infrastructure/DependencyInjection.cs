using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SensorIngestion.Application.Persistence;
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
        return services;
    }
}
