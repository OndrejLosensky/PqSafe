namespace PgSafe.Models.Backup;

public class BackupMetadata
{
    public string Instance { get; init; } = null!;
    public string Database { get; init; } = null!;
    public string BackupId { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public long SizeBytes { get; init; }
    public string Format { get; init; } = "custom";
    public string PgVersion { get; init; } = "unknown";
    public int TableCount { get; set; }
    public long RowCount { get; set; }
}