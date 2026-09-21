using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SensorIngestion.Api.Contracts.Responses;
using SensorIngestion.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace SensorIngestion.IntegrationTests.Api;

public sealed class IngestionsEndpointTests
{
    private const string ValidReadings = """
        {"deviceId":"PUMP-01","metric":"temperature","ts":"2025-06-01T08:00:00Z","value":70,"seq":1}
        {"deviceId":"PUMP-01","metric":"temperature","ts":"2025-06-01T08:00:10Z","value":71,"seq":2}
        """;

    [Fact]
    public async Task Swagger_ShouldExposeInteractiveUiAndIngestionContract()
    {
        await using var fixture = await ApiFixture.CreateAsync();

        var uiResponse = await fixture.Client.GetAsync("swagger/index.html");
        var documentResponse = await fixture.Client.GetAsync("swagger/v1/swagger.json");
        var document = await documentResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);
        Assert.Contains("/api/ingestions", document);
        Assert.Contains("multipart/form-data", document);
    }

    [Fact]
    public async Task Post_WhenFileIsValid_ShouldProcessItAndReturnReport()
    {
        await using var fixture = await ApiFixture.CreateAsync();

        var response = await fixture.PostAsync(ValidReadings, "readings.json");
        var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(2, result.Report.TotalLinesRead);
        Assert.Equal(2, result.Report.StoredReadings);
        Assert.Equal(2, result.Report.AcceptableReadings);
        Assert.Empty(result.Rejections);
        Assert.Empty(result.Alerts);
        Assert.Equal(2, await fixture.CountReadingsAsync());
    }

    [Fact]
    public async Task Post_WhenSameFileIsUploadedAgain_ShouldRemainIdempotent()
    {
        await using var fixture = await ApiFixture.CreateAsync();

        var firstResponse = await fixture.PostAsync(ValidReadings, "readings.jsonl");
        var secondResponse = await fixture.PostAsync(ValidReadings, "readings.jsonl");
        var first = await firstResponse.Content.ReadFromJsonAsync<IngestionResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<IngestionResponse>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(2, first!.Report.StoredReadings);
        Assert.Equal(0, second!.Report.StoredReadings);
        Assert.Equal(2, await fixture.CountReadingsAsync());
    }

    [Fact]
    public async Task Post_WhenFileContainsRejectedLine_ShouldReturnItsLineNumberAndReason()
    {
        await using var fixture = await ApiFixture.CreateAsync();
        const string readings = """
            {"deviceId":"PUMP-01","metric":"temperature","ts":"2025-06-01T08:00:00Z","value":70,"seq":1}
            not-json
            """;

        var response = await fixture.PostAsync(readings, "readings.json");
        var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rejection = Assert.Single(result!.Rejections);
        Assert.Equal(2, rejection.LineNumber);
        Assert.Equal("MalformedInput", rejection.Category);
        Assert.False(string.IsNullOrWhiteSpace(rejection.Reason));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Post_WhenFileIsMissingOrEmpty_ShouldReturnBadRequest(string? content)
    {
        await using var fixture = await ApiFixture.CreateAsync();
        using var request = new MultipartFormDataContent();
        if (content is not null)
            request.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", "readings.json");

        var response = await fixture.Client.PostAsync("api/ingestions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await fixture.CountReadingsAsync());
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
            var directory = Path.Combine(Path.GetTempPath(), $"sensor-ingestion-upload-api-{Guid.NewGuid():N}");
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

        public async Task<HttpResponseMessage> PostAsync(string content, string fileName)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", fileName);
            return await Client.PostAsync("api/ingestions", form);
        }

        public async Task<int> CountReadingsAsync()
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<SensorIngestionDbContext>().Readings.CountAsync();
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
