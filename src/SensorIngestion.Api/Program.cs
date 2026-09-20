using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Operators;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Infrastructure.RuleConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

var app = builder.Build();

var ruleLoader = app.Services.GetRequiredService<IRuleConfigurationLoader>();
var ruleDefinitions = await ruleLoader.LoadAsync(CancellationToken.None);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
