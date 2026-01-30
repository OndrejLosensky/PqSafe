using PgSafe.Config;

namespace PgSafe.Models.Restore;

public class RestoreTarget
{
    public string InstanceName { get; }
    public PgInstanceConfig InstanceConfig { get; }
    public string DatabaseName { get; }
    public string DumpFile { get; }

    public RestoreTarget(
        string instanceName,
        PgInstanceConfig instanceConfig,
        string databaseName,
        string dumpFile
    )
    {
        InstanceName = instanceName;
        InstanceConfig = instanceConfig;
        DatabaseName = databaseName;
        DumpFile = dumpFile;
    }
}