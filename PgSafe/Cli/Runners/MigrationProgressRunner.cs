using PgSafe.Config;
using PgSafe.Models.Migration;
using PgSafe.Models.Backup;
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

            // Label (shown in progress UI)
            t => $"{t.SourceInstanceName}/{t.SourceDatabaseName} → {t.TargetInstanceName}/{t.TargetDatabaseName}",

            // Execute
            t =>
            {
                if (t.DryRun)
                    return;

                // --- BACKUP ---
                var backupSet = BackupService.RunSingle(
                    t.OutputDir,
                    t.SourceInstanceName,
                    t.SourceInstanceConfig,
                    t.SourceDatabaseName
                );

                // --- RESTORE ---
                RestoreService.RunSingle(
                    t.TargetInstanceName,
                    t.TargetInstanceConfig,
                    t.TargetDatabaseName,
                    backupSet.DumpPath
                );
            },

            // Success
            (t, duration) =>
            {
                // In dry-run we don’t have a real file
                string? dumpPath = t.DryRun
                    ? null
                    : Path.Combine(
                        t.OutputDir,
                        t.SourceInstanceName,
                        t.SourceDatabaseName
                    );

                result.Successes.Add(new MigrationSuccess
                {
                    // PgTaskResult = TARGET
                    Instance = t.TargetInstanceName,
                    Database = t.TargetDatabaseName,
                    FilePath = dumpPath,
                    FileSizeBytes = dumpPath is not null && File.Exists(dumpPath)
                        ? FileUtils.GetFileSize(dumpPath)
                        : null,
                    Duration = duration,

                    // Migration-specific
                    SourceInstance = t.SourceInstanceName,
                    SourceDatabase = t.SourceDatabaseName
                });
            },

            // Failure
            (t, ex, duration) =>
            {
                result.Failures.Add(new MigrationFailure
                {
                    // PgTaskResult = TARGET
                    Instance = t.TargetInstanceName,
                    Database = t.TargetDatabaseName,
                    Duration = duration,

                    // Migration-specific
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
