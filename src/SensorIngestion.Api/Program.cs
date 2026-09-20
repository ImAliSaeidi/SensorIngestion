using SensorIngestion.Application.Alerting;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Operators;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Infrastructure;
using SensorIngestion.Infrastructure.Persistence;
using SensorIngestion.Infrastructure.RuleConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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
builder.Services.AddSingleton<RuleOperatorRegistry>();
builder.Services.AddSingleton<StatelessRuleEvaluator>();
builder.Services.AddSingleton<SustainedAboveEvaluator>();
builder.Services.AddSingleton<AlertGenerator>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SensorIngestionDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var ruleLoader = scope.ServiceProvider.GetRequiredService<IRuleConfigurationLoader>();
    var ruleDefinitions = await ruleLoader.LoadAsync(CancellationToken.None);
    var ruleCatalog = scope.ServiceProvider.GetRequiredService<SensorIngestion.Application.Persistence.IRuleCatalog>();
    await ruleCatalog.SynchronizeAsync(ruleDefinitions, DateTimeOffset.UtcNow, CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
