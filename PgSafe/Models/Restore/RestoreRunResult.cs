namespace PgSafe.Models.Restore;

public record RestoreSuccess(
    string Instance,
    string Database,
    string FilePath
);

public record RestoreFailure(
    string Instance,
    string Database,
    string Error
);

public class RestoreRunResult
{
    public List<RestoreSuccess> Successes { get; } = new List<RestoreSuccess>();
    public List<RestoreFailure> Failures { get; } = new List<RestoreFailure>();

    public bool HasFailures => Failures.Count > 0;
}