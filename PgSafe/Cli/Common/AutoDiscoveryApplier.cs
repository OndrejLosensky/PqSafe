using PgSafe.Config;
using PgSafe.Services;

namespace PgSafe.Cli.Common;

public static class AutoDiscoveryApplier
{
    public static void Apply(PgSafeConfig config)
    {
        foreach (var (_, instance) in config.Instances)
        {
            if (!instance.AutoDetect)
                continue;

            var databases = DatabaseDiscoveryService.DiscoverDatabases(instance);

            instance.Databases.Clear();

            foreach (var db in databases)
            {
                instance.Databases[db] = new PgDatabaseConfig();
            }
        }
    }
}