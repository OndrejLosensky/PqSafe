namespace PgSafe.Models.Backup;

public record BackupProgress(
    string Instance,
    string Database,
    int Current,
    int Total
);