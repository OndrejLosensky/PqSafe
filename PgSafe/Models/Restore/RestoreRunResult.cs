namespace PgSafe.Models.Restore;

public record RestoreSuccess : PgTaskResult;

public record RestoreFailure : PgTaskResult
{
    public required string Error { get; init; }

    // Longer diagnostic: exception stack trace, inner exception, etc.
    public string? Details { get; init; }

    // Where we wrote Details (helpful when Details is too long for the table)
    public string? LogFilePath { get; init; }
}

public class RestoreRunResult
{
    public List<RestoreSuccess> Successes { get; } = new List<RestoreSuccess>();
    public List<RestoreFailure> Failures { get; } = new List<RestoreFailure>();

    public bool HasFailures => Failures.Count > 0;
}