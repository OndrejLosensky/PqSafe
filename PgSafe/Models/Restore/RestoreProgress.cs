namespace PgSafe.Models.Restore;

public record RestoreProgress(
    string Instance,
    string Database,
    int Current,
    int Total
);