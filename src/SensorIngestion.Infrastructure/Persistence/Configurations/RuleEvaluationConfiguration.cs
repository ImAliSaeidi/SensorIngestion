using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Domain.Rules.Evaluations;

namespace SensorIngestion.Infrastructure.Persistence.Configurations;

public class RuleEvaluationConfiguration : IEntityTypeConfiguration<RuleEvaluation>
{
    public void Configure(EntityTypeBuilder<RuleEvaluation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Outcome)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(512);

        builder.Property(x => x.EvaluatedAt)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));

        builder.HasOne<SensorReading>()
            .WithMany()
            .HasForeignKey(x => x.SensorReadingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.SensorReadingId,
            x.RuleId
        }).IsUnique();
    }
}
