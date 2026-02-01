namespace PgSafe.Models.Backup;

public class BackupSet
{
    public string Instance { get; init; } = null!;
    public string Database { get; init; } = null!;
    public string BackupId { get; init; } = null!;

    public string BackupDirectory { get; init; } = null!;
    public string DumpPath { get; init; } = null!;
    public string MetaPath { get; init; } = null!;

    public BackupMetadata Metadata { get; init; } = null!;
}