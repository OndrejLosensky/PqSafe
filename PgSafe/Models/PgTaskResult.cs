namespace PgSafe.Models;

public abstract record PgTaskResult
{
    public required string Instance { get; init; }
    public required string Database { get; init; }
    public string? FilePath { get; init; }
    public long? FileSizeBytes { get; init; }
    public TimeSpan Duration { get; init; }
}
