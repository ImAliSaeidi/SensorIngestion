using Microsoft.AspNetCore.Mvc;
using SensorIngestion.Api.Contracts.Requests;
using SensorIngestion.Api.Contracts.Responses;
using SensorIngestion.Application.Aggregation;
using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Api.Controllers;

[ApiController]
[Route("api/aggregations")]
public sealed class AggregationsController(GetReadingAggregates aggregates) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AggregationBucketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AggregationBucketResponse>>> GetAsync([FromQuery] AggregationRequest request, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return BadRequest(new ValidationProblemDetails(errors));

        var query = new AggregationQuery(
            request.DeviceId!.Trim(),
            Metric.Create(request.Metric!),
            request.From!.Value,
            request.To!.Value,
            request.BucketSeconds!.Value);

        var buckets = await aggregates.ExecuteAsync(query, cancellationToken);
        return Ok(buckets.Select(x => new AggregationBucketResponse(x.Start, x.Count, x.Average, x.Minimum, x.Maximum)).ToArray());
    }

    private static Dictionary<string, string[]> Validate(AggregationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DeviceId))
            errors[nameof(request.DeviceId)] = ["deviceId is required."];

        if (string.IsNullOrWhiteSpace(request.Metric))
            errors[nameof(request.Metric)] = ["metric is required."];

        if (request.From is null || request.From.Value.Offset != TimeSpan.Zero)
            errors[nameof(request.From)] = ["from must be an ISO-8601 UTC timestamp."];

        if (request.To is null || request.To.Value.Offset != TimeSpan.Zero)
            errors[nameof(request.To)] = ["to must be an ISO-8601 UTC timestamp."];

        if (request.From is not null && request.To is not null && request.From >= request.To)
            errors[nameof(request.To)] = ["to must be later than from."];

        if (request.BucketSeconds is null or <= 0)
            errors[nameof(request.BucketSeconds)] = ["bucketSeconds must be greater than zero."];

        return errors;
    }
}
