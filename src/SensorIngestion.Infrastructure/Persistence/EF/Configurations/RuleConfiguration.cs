using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Infrastructure.Persistence.EF.Configurations;

internal class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.RuleKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Metric)
            .HasConversion(
                metric => metric.Value,
                value => Metric.Create(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.DeviceId)
            .HasMaxLength(64);

        builder.Property(x => x.Operator)
            .HasConversion(
                operation => operation.Value,
                value => RuleOperator.Create(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ConfigurationHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));

        builder.HasIndex(x => new
        {
            x.RuleKey,
            x.Version
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.RuleKey,
            x.ConfigurationHash
        }).IsUnique();

        builder.OwnsMany(x => x.Parameters, parameter =>
        {
            parameter.WithOwner().HasForeignKey("RuleId");

            parameter.Property(x => x.Name)
                .HasMaxLength(64)
                .IsRequired();

            parameter.Property(x => x.Value)
                .IsRequired();

            parameter.HasKey("RuleId", nameof(RuleParameter.Name));
        });

        builder.Navigation(x => x.Parameters)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.DeviceId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Metric)
            .HasConversion(
                metric => metric.Value,
                value => Metric.Create(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.StartTimestamp)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));

        builder.Property(x => x.EndTimestamp)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));

        builder.Property(x => x.CreatedAt)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));

        builder.Ignore(x => x.Identity);

        builder.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.RuleId,
            x.DeviceId,
            x.Metric,
            x.StartTimestamp
        }).IsUnique();

    }
}
