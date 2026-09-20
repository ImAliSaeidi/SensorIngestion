using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.UnitTests.Domain.Readings;

public sealed class ReadingIdentityTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 6, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Equality_WhenAllComponentsMatch_ShouldBeEqual()
    {
        var first = CreateIdentity();
        var second = CreateIdentity();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equality_WhenAnyComponentDiffers_ShouldNotBeEqual()
    {
        var identity = CreateIdentity();

        Assert.NotEqual(identity, identity with { DeviceId = "PUMP-02" });
        Assert.NotEqual(identity, identity with { Metric = Metric.Pressure });
        Assert.NotEqual(identity, identity with { Timestamp = Timestamp.AddSeconds(1) });
        Assert.NotEqual(identity, identity with { Sequence = 2 });
    }

    [Fact]
    public void HashSet_WhenIdentityIsDuplicated_ShouldKeepSingleValue()
    {
        var identities = new HashSet<ReadingIdentity> { CreateIdentity(), CreateIdentity() };

        Assert.Single(identities);
    }

    [Fact]
    public void Equality_WhenDeviceIdCaseDiffers_ShouldNotBeEqual()
    {
        var first = CreateIdentity();
        var second = first with { DeviceId = "pump-01" };

        Assert.NotEqual(first, second);
    }

    private static ReadingIdentity CreateIdentity() => new("PUMP-01", Metric.Temperature, Timestamp, 1);
}
