namespace PgSafe.Models.Restore;

public record RestoreSuccess : PgTaskResult;

public record RestoreFailure : PgTaskResult
{
    public required string Error { get; init; }
}

public class RestoreRunResult
{
    public List<RestoreSuccess> Successes { get; } = new List<RestoreSuccess>();
    public List<RestoreFailure> Failures { get; } = new List<RestoreFailure>();

    public bool HasFailures => Failures.Count > 0;
}