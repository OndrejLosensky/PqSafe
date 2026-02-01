using PgSafe.Config;

namespace PgSafe.Models.Backup;

public class BackupTarget
{
    public string InstanceName { get; }
    public PgInstanceConfig InstanceConfig { get; }
    public string DatabaseName { get; }

    // Runtime result: the completed backup
    public BackupSet? BackupSet { get; set; }

    public BackupTarget(
        string instanceName,
        PgInstanceConfig instanceConfig,
        string databaseName
    )
    {
        InstanceName = instanceName;
        InstanceConfig = instanceConfig;
        DatabaseName = databaseName;
    }
}