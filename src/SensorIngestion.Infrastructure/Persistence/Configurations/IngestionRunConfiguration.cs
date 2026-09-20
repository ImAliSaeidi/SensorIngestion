using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SensorIngestion.Infrastructure.Persistence.Configurations;

internal sealed class IngestionRunConfiguration : IEntityTypeConfiguration<IngestionRunRecord>
{
    public void Configure(EntityTypeBuilder<IngestionRunRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.FileFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1024);
        builder.Property(x => x.StartedAt).HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
        builder.Property(x => x.CompletedAt).HasConversion(x => x.HasValue ? x.Value.UtcTicks : (long?)null, x => x.HasValue ? new DateTimeOffset(x.Value, TimeSpan.Zero) : null);
        builder.HasIndex(x => x.FileFingerprint);
    }
}
