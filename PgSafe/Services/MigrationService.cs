using System.Diagnostics;
using PgSafe.Config;
using PgSafe.Models.Migration;
using PgSafe.Utils;

namespace PgSafe.Services;

public static class MigrationService
{
    public static MigrationRunResult MigrateDatabase(
        PgSafeConfig config,
        string sourceInstanceName,
        string sourceDbName,
        string targetInstanceName,
        string targetDbName
    )
    {
        var result = new MigrationRunResult();
        var sw = Stopwatch.StartNew();

        try
        {
            // ----------------------
            // BACKUP
            // ----------------------
            var sourceInstance = config.Instances[sourceInstanceName];

            var backupSet = BackupService.RunSingle(
                config.OutputDir,
                sourceInstanceName,
                sourceInstance,
                sourceDbName
            );

            // ----------------------
            // RESTORE
            // ----------------------
            var targetInstance = config.Instances[targetInstanceName];

            RestoreService.RunSingle(
                targetInstanceName,
                targetInstance,
                targetDbName,
                backupSet.DumpPath
            );

            sw.Stop();

            result.Successes.Add(new MigrationSuccess
            {
                // PgTaskResult (TARGET)
                Instance = targetInstanceName,
                Database = targetDbName,
                FilePath = backupSet.DumpPath,
                FileSizeBytes = FileUtils.GetFileSize(backupSet.DumpPath),
                Duration = sw.Elapsed,

                // Migration-specific
                SourceInstance = sourceInstanceName,
                SourceDatabase = sourceDbName
            });
        }
        catch (Exception ex)
        {
            sw.Stop();

            result.Failures.Add(new MigrationFailure
            {
                // PgTaskResult (TARGET)
                Instance = targetInstanceName,
                Database = targetDbName,
                Duration = sw.Elapsed,

                // Migration-specific
                SourceInstance = sourceInstanceName,
                SourceDatabase = sourceDbName,

                Error = ex.Message,
                Details = ex.StackTrace
            });
        }

        return result;
    }
}
