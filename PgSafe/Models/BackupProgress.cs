namespace PgSafe.Models;

public record BackupProgress(
    string Instance,
    string Database,
    int Current,
    int Total
);