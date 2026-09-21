using Microsoft.EntityFrameworkCore;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Domain.Rules.Evaluations;

namespace SensorIngestion.Infrastructure.Persistence.EF;

public sealed class SensorIngestionDbContext(DbContextOptions<SensorIngestionDbContext> options) : DbContext(options)
{
    public DbSet<SensorReading> Readings => Set<SensorReading>();

    public DbSet<Rule> Rules => Set<Rule>();

    public DbSet<RuleEvaluation> RuleEvaluations => Set<RuleEvaluation>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<IngestionRunRecord> IngestionRuns => Set<IngestionRunRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SensorIngestionDbContext).Assembly);
    }
}
