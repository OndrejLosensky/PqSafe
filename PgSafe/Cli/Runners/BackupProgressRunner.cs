using PgSafe.Config;
using PgSafe.Models.Backup;
using PgSafe.Services;
using PgSafe.Utils;

namespace PgSafe.Cli.Runners;

public static class BackupProgressRunner
{
    public static BackupRunResult Run(PgSafeConfig config)
    {
        var targets = BuildTargets(config);
        return RunInternal(config, targets);
    }

    public static BackupRunResult RunSingle(
        PgSafeConfig config,
        string instanceName,
        PgInstanceConfig instance,
        string database
    )
    {
        return RunInternal(
            config,
            new[]
            {
                new BackupTarget(instanceName, instance, database)
            }
        );
    }

    private static BackupRunResult RunInternal(
        PgSafeConfig config,
        IEnumerable<BackupTarget> targets
    )
    {
        var result = new BackupRunResult();

        ProgressRunner.Run(
            targets,
            t => config.DryRun
                ? $"[grey](dry-run)[/] {t.InstanceName}/{t.DatabaseName}"
                : $"{t.InstanceName}/{t.DatabaseName}",

            t =>
            {
                if (config.DryRun)
                    return;

                t.FilePath = BackupService.RunSingle(
                    config.OutputDir,
                    t.InstanceName,
                    t.InstanceConfig,
                    t.DatabaseName
                );
            },

            (t, d) => result.Successes.Add(new BackupSuccess
            {
                Instance = t.InstanceName,
                Database = t.DatabaseName,
                FilePath = t.FilePath,
                FileSizeBytes = t.FilePath != null
                    ? FileUtils.GetFileSize(t.FilePath)
                    : null,
                Duration = d
            }),

            (t, ex, d) => result.Failures.Add(new BackupFailure
            {
                Instance = t.InstanceName,
                Database = t.DatabaseName,
                Error = ex.Message,
                Duration = d
            })
        );

        return result;
    }

    private static IEnumerable<BackupTarget> BuildTargets(PgSafeConfig config)
    {
        foreach (var (instanceName, instance) in config.Instances)
        {
            foreach (var (dbName, db) in instance.Databases)
            {
                if (db.Backup.Enabled)
                    yield return new BackupTarget(
                        instanceName,
                        instance,
                        dbName
                    );
            }
        }
    }
}
