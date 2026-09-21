using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Infrastructure.Persistence.EF.Configurations;

internal class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
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

        builder.Property(x => x.Classification)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.Timestamp)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));

        builder.Ignore(x => x.Identity);

        builder.HasIndex(x => new
        {
            x.DeviceId,
            x.Metric,
            x.Timestamp,
            x.Sequence
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.DeviceId,
            x.Metric,
            x.Classification,
            x.Timestamp
        });
    }
}
