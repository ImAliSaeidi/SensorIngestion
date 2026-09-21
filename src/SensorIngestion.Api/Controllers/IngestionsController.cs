using Microsoft.AspNetCore.Mvc;
using SensorIngestion.Api.Contracts.Requests;
using SensorIngestion.Api.Contracts.Responses;
using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.Api.Controllers;

[ApiController]
[Route("api/ingestions")]
public sealed class IngestionsController(IngestionProcessor processor, IReadingSourceFactory sourceFactory) : ControllerBase
{
    private const long MaximumFileSize = 100 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IngestionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(MaximumFileSize)]
    public async Task<ActionResult<IngestionResponse>> CreateAsync([FromForm] IngestionUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "A non-empty readings file is required.");
            return ValidationProblem(ModelState);
        }

        var temporaryPath = Path.Combine(Path.GetTempPath(), $"sensor-ingestion-{Guid.NewGuid():N}.jsonl");

        try
        {
            await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await request.File.CopyToAsync(destination, cancellationToken);

            var result = await processor.ProcessAsync(sourceFactory.Create(temporaryPath), cancellationToken);
            return Ok(Map(result));
        }
        finally
        {
            if (System.IO.File.Exists(temporaryPath))
                System.IO.File.Delete(temporaryPath);
        }
    }

    private static IngestionResponse Map(IngestionResult result)
    {
        var report = result.Report;
        return new IngestionResponse(
            new ProcessingReportResponse(
                report.TotalLinesRead,
                report.ParsedReadings,
                report.StoredReadings,
                report.DuplicatesRemoved,
                report.InvalidRecordsRejected,
                report.RulesLoaded,
                report.RuleEvaluationsPerformed,
                report.AcceptableReadings,
                report.UnacceptableReadings,
                report.RuleViolations,
                report.AlertsGenerated),
            result.Rejections.Select(x => new ReadingRejectionResponse(x.LineNumber, x.Category.ToString(), x.Reason, x.FieldName)).ToArray(),
            result.Alerts.Select(x => new AlertResponse(x.Id, x.RuleId, x.DeviceId, x.Metric.Value, x.StartTimestamp, x.EndTimestamp, x.PeakValue, x.IsOpen, x.CreatedAt)).ToArray());
    }
}
