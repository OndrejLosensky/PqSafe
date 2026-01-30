namespace PgSafe.Models.Backup;

public record BackupSuccess : PgTaskResult;

public record BackupFailure : PgTaskResult
{
    public required string Error { get; init; }
}

public class BackupRunResult
{
    public List<BackupSuccess> Successes { get; } = new();
    public List<BackupFailure> Failures { get; } = new();

    public bool HasFailures => Failures.Count > 0;
}
