using PgSafe.Config;

namespace PgSafe.Cli.Common;

public static class SelectionApplier
{
    public static void ApplyDatabaseSelection(
        PgSafeConfig config,
        List<(string instance, string database)> selected
    )
    {
        var selectedSet = selected.ToHashSet();

        foreach (var (instanceName, instance) in config.Instances)
        {
            foreach (var (dbName, db) in instance.Databases)
            {
                db.Backup.Enabled =
                    selectedSet.Contains((instanceName, dbName));
            }
        }
    }
}