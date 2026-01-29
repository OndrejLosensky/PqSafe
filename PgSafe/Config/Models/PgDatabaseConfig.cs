namespace PgSafe.Config;

public class PgDatabaseConfig
{
    // future-proof section
    public PgBackupConfig Backup { get; set; } = new();
}