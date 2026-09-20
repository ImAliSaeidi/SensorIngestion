using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SensorIngestion.Api.Contracts.Responses;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;

namespace SensorIngestion.IntegrationTests.Api;

public sealed class AggregationsEndpointTests
{
    private static readonly DateTimeOffset From = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Get_WhenRequestIsValid_ShouldReturnAcceptableHalfOpenBuckets()
    {
        await using var fixture = await ApiFixture.CreateAsync();
        await fixture.SeedAsync(
            Reading(From, 10, acceptable: true, sequence: 1),
            Reading(From.AddSeconds(59), 20, acceptable: true, sequence: 2),
            Reading(From.AddSeconds(60), 30, acceptable: true, sequence: 3),
            Reading(From.AddSeconds(30), 999, acceptable: false, sequence: 4),
            Reading(From.AddMinutes(2), 999, acceptable: true, sequence: 5));

        var response = await fixture.Client.GetAsync("api/aggregations?deviceId=PUMP-01&metric=temperature&from=2025-06-01T08:00:00Z&to=2025-06-01T08:02:00Z&bucketSeconds=60");
        var buckets = await response.Content.ReadFromJsonAsync<AggregationBucketResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Collection(buckets!,
            bucket => Assert.Equal((From, 2, 15d, 10d, 20d), (bucket.Start, bucket.Count, bucket.Average, bucket.Minimum, bucket.Maximum)),
            bucket => Assert.Equal((From.AddMinutes(1), 1, 30d, 30d, 30d), (bucket.Start, bucket.Count, bucket.Average, bucket.Minimum, bucket.Maximum)));
    }

    [Theory]
    [InlineData("api/aggregations?deviceId=PUMP-01&metric=temperature&from=2025-06-01T08:00:00Z&to=2025-06-01T08:02:00Z&bucketSeconds=0")]
    [InlineData("api/aggregations?deviceId=PUMP-01&metric=temperature&from=2025-06-01T08:02:00Z&to=2025-06-01T08:00:00Z&bucketSeconds=60")]
    [InlineData("api/aggregations?deviceId=PUMP-01&metric=temperature&from=2025-06-01T11:30:00%2B03:30&to=2025-06-01T11:32:00%2B03:30&bucketSeconds=60")]
    public async Task Get_WhenRequestIsInvalid_ShouldReturnBadRequest(string url)
    {
        await using var fixture = await ApiFixture.CreateAsync();

        var response = await fixture.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static SensorReading Reading(DateTimeOffset timestamp, double value, bool acceptable, long sequence)
    {
        var reading = new SensorReading("PUMP-01", Metric.Temperature, timestamp, value, sequence);
        reading.Classify(hasViolation: !acceptable);
        return reading;
    }

    private sealed class ApiFixture : IAsyncDisposable
    {
        private readonly string _directory;
        private readonly WebApplicationFactory<Program> _factory;

        public HttpClient Client { get; }

        private ApiFixture(string directory, WebApplicationFactory<Program> factory, HttpClient client)
        {
            _directory = directory;
            _factory = factory;
            Client = client;
        }

        public static async Task<ApiFixture> CreateAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sensor-ingestion-api-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var rulesPath = Path.Combine(directory, "rules.json");
            var databasePath = Path.Combine(directory, "test.db");
            await File.WriteAllTextAsync(rulesPath, "[]");

            var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RuleConfiguration:Path"] = rulesPath,
                    ["Persistence:DatabasePath"] = databasePath,
                    ["Input:Path"] = Path.Combine(directory, "unused.jsonl"),
                    ["Input:ProcessOnStartup"] = "false"
                }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<SensorIngestionDbContext>>();
                    services.RemoveAll<SensorIngestionDbContext>();
                    services.AddDbContext<SensorIngestionDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
                });
            });

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            return new ApiFixture(directory, factory, client);
        }

        public async Task SeedAsync(params SensorReading[] readings)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<SensorIngestionDbContext>();
            context.Readings.AddRange(readings);
            await context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
            SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, recursive: true);
        }
    }
}
