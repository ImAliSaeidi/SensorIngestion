using SensorIngestion.Application.Abstractions.Persistence;
using SensorIngestion.Application.Abstractions.Rules.Configuration;
using SensorIngestion.Application.Abstractions.Rules.Evaluation;
using SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;
using SensorIngestion.Application.Alerting;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Operators;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Infrastructure;
using SensorIngestion.Infrastructure.Persistence.EF;
using SensorIngestion.Infrastructure.RuleConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton<IRuleConfigurationLoader>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var configuredPath = configuration["RuleConfiguration:Path"];

    if (string.IsNullOrWhiteSpace(configuredPath))
        throw new InvalidOperationException("Rule configuration path is not configured.");

    var resolvedPath = Path.IsPathRooted(configuredPath)
        ? Path.GetFullPath(configuredPath)
        : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);

    return new JsonRuleConfigurationLoader(resolvedPath);
});

builder.Services.AddSingleton<IRuleOperatorStrategy, BetweenOperatorStrategy>();
builder.Services.AddSingleton<IRuleOperatorStrategy, EqualOperatorStrategy>();
builder.Services.AddSingleton<IRuleOperatorStrategy, GreaterThanOperatorStrategy>();
builder.Services.AddSingleton<IRuleOperatorStrategy, GreaterThanOrEqualOperatorStrategy>();
builder.Services.AddSingleton<IRuleOperatorStrategy, LessThanOperatorStrategy>();
builder.Services.AddSingleton<IRuleOperatorStrategy, LessThanOrEqualOperatorStrategy>();
builder.Services.AddSingleton<IStatefulRuleEvaluator, SustainedAboveEvaluator>();
builder.Services.AddSingleton<RuleOperatorRegistry>();
builder.Services.AddSingleton<StatefulRuleEvaluatorRegistry>();
builder.Services.AddSingleton<StatelessRuleEvaluator>();
builder.Services.AddSingleton<RuleEngine>();
builder.Services.AddSingleton<AlertGenerator>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SensorIngestionDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var ruleLoader = scope.ServiceProvider.GetRequiredService<IRuleConfigurationLoader>();
    var ruleDefinitions = await ruleLoader.LoadAsync(CancellationToken.None);
    var ruleCatalog = scope.ServiceProvider.GetRequiredService<IRuleCatalog>();
    await ruleCatalog.SynchronizeAsync(ruleDefinitions, DateTimeOffset.UtcNow, CancellationToken.None);

    if (builder.Configuration.GetValue<bool>("Input:ProcessOnStartup"))
    {
        var processor = scope.ServiceProvider.GetRequiredService<SensorIngestion.Application.Ingestion.IngestionProcessor>();
        var result = await processor.ProcessAsync(CancellationToken.None);
        Console.WriteLine(result.Report);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).ExcludeFromDescription();

app.Run();

public partial class Program;
