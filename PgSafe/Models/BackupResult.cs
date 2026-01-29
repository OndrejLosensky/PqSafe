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
    public List<BackupSuccess> Successes { get; } = new();
    public List<BackupFailure> Failures { get; } = new();

    public bool HasFailures => Failures.Count > 0;
}
