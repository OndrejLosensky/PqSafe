namespace PgSafe.Config;

public class PgBackupConfig
{
    public bool Enabled { get; set; } = true;

    // optional for later
    public bool SchemaOnly { get; set; } = false;
}