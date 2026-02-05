using PgSafe.Config;
using PgSafe.Models.Migration;
using PgSafe.Services;
using PgSafe.Utils;

namespace PgSafe.Cli.Runners;

public static class MigrationProgressRunner
{
    public static MigrationRunResult Run(
        PgSafeConfig config,
        string sourceInstanceName,
        string sourceDbName,
        string targetInstanceName,
        string targetDbName
    )
    {
        var result = new MigrationRunResult();

        var target = new MigrationTarget(
            sourceInstanceName,
            sourceDbName,
            config.Instances[sourceInstanceName],

            targetInstanceName,
            targetDbName,
            config.Instances[targetInstanceName],

            config.OutputDir,
            config.DryRun
        );

        ProgressRunner.Run(
            new[] { target },
            t => $"{t.SourceInstanceName}/{t.SourceDatabaseName} → {t.TargetInstanceName}/{t.TargetDatabaseName}",
            (t, report) =>
            {
                const double CreateDbWeight = 5;
                const double BackupWeight = 35;
                const double RestoreWeight = 60;

                const double AfterCreateDb = CreateDbWeight;                 // 5
                const double AfterBackup = CreateDbWeight + BackupWeight;    // 40
                const double AfterRestore = 100;                             // 100

                report(0);

                if (t.DryRun)
                {
                    report(AfterCreateDb);
                    report(AfterBackup);
                    report(AfterRestore);
                    return;
                }

                // --- CREATE DB (if missing) ---
                if (!DatabaseUtils.DatabaseExists(t.TargetInstanceConfig, t.TargetDatabaseName))
                {
                    DatabaseProvisioningService.CreateDatabase(
                        t.TargetInstanceConfig,
                        t.TargetDatabaseName
                    );
                }

                report(AfterCreateDb);

                // --- BACKUP ---
                var backupSet = BackupService.RunSingle(
                    t.OutputDir,
                    t.SourceInstanceName,
                    t.SourceInstanceConfig,
                    t.SourceDatabaseName
                );

                report(AfterBackup);

                // --- RESTORE ---
                RestoreService.RunSingle(
                    t.TargetInstanceName,
                    t.TargetInstanceConfig,
                    t.TargetDatabaseName,
                    backupSet.DumpPath
                );

                report(AfterRestore);
            },
            (t, duration) =>
            {
                string? dumpPath = t.DryRun
                    ? null
                    : Path.Combine(
                        t.OutputDir,
                        t.SourceInstanceName,
                        t.SourceDatabaseName
                    );

                result.Successes.Add(new MigrationSuccess
                {
                    Instance = t.TargetInstanceName,
                    Database = t.TargetDatabaseName,
                    FilePath = dumpPath,
                    FileSizeBytes = dumpPath is not null && File.Exists(dumpPath)
                        ? FileUtils.GetFileSize(dumpPath)
                        : null,
                    Duration = duration,
                    SourceInstance = t.SourceInstanceName,
                    SourceDatabase = t.SourceDatabaseName
                });
            },
            (t, ex, duration) =>
            {
                result.Failures.Add(new MigrationFailure
                {
                    Instance = t.TargetInstanceName,
                    Database = t.TargetDatabaseName,
                    Duration = duration,
                    SourceInstance = t.SourceInstanceName,
                    SourceDatabase = t.SourceDatabaseName,
                    Error = ex.Message,
                    Details = ex.ToString()
                });
            }
        );

        return result;
    }
}