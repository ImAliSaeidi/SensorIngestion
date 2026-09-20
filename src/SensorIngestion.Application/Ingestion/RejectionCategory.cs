namespace SensorIngestion.Application.Ingestion;

public enum RejectionCategory
{
    EmptyLine,

    MalformedInput,

    InvalidRoot,

    MissingRequiredField,

    InvalidFieldType,

    InvalidFieldValue,

    InvalidTimestamp
}