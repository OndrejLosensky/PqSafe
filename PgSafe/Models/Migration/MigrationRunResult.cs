using PgSafe.Models;

namespace PgSafe.Models.Migration;

public record MigrationSuccess : PgTaskResult
{
    public required string SourceInstance { get; init; }
    public required string SourceDatabase { get; init; }
}

public record MigrationFailure : PgTaskResult
{
    public required string SourceInstance { get; init; }
    public required string SourceDatabase { get; init; }

    public required string Error { get; init; }

    // Optional deep diagnostics
    public string? Details { get; init; }
    public string? LogFilePath { get; init; }
}

public class MigrationRunResult
{
    public List<MigrationSuccess> Successes { get; } = new();
    public List<MigrationFailure> Failures { get; } = new();

    public bool HasFailures => Failures.Count > 0;
}