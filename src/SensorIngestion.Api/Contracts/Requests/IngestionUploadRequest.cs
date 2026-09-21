using System.ComponentModel.DataAnnotations;

namespace SensorIngestion.Api.Contracts.Requests;

public sealed class IngestionUploadRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
