namespace PgSafe.Models;

public record BackupSuccess(
    string Instance,
    string Database,
    string FilePath
);

public record BackupFailure(
    string Instance,
    string Database,
    string Error
);

public class BackupRunResult
{
    public List<BackupSuccess> Successes { get; } = [];
    public List<BackupFailure> Failures { get; } = [];

    public bool HasFailures => Failures.Count > 0;
}