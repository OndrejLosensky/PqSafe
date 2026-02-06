using System.Diagnostics;
using PgSafe.Config;
using PgSafe.Models;
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

        // Captured from the execute lambda; one target => this is fine.
        var steps = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
        var skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ProgressRunner.Run(
            new[] { target },
            t => $"{t.SourceInstanceName}/{t.SourceDatabaseName} → {t.TargetInstanceName}/{t.TargetDatabaseName}",
            (t, report, setStep) =>
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
                    setStep(StepKeys.ToLabel(StepKeys.EnsureDb));
                    skipped.Add(StepKeys.EnsureDb);
                    report(AfterCreateDb);

                    setStep(StepKeys.ToLabel(StepKeys.Backup));
                    skipped.Add(StepKeys.Backup);
                    report(AfterBackup);

                    setStep(StepKeys.ToLabel(StepKeys.Restore));
                    skipped.Add(StepKeys.Restore);
                    report(AfterRestore);

                    return;
                }

                // ----------------------
                // ENSURE DB (create if missing)
                // ----------------------
                setStep(StepKeys.ToLabel(StepKeys.EnsureDb));
                var swEnsure = Stopwatch.StartNew();

                var exists = DatabaseUtils.DatabaseExists(t.TargetInstanceConfig, t.TargetDatabaseName);
                if (!exists)
                {
                    DatabaseProvisioningService.CreateDatabase(
                        t.TargetInstanceConfig,
                        t.TargetDatabaseName
                    );

                    swEnsure.Stop();
                    steps[StepKeys.EnsureDb] = swEnsure.Elapsed;
                }
                else
                {
                    swEnsure.Stop();
                    skipped.Add(StepKeys.EnsureDb);
                }

                report(AfterCreateDb);

                // ----------------------
                // BACKUP
                // ----------------------
                setStep(StepKeys.ToLabel(StepKeys.Backup));
                var swBackup = Stopwatch.StartNew();

                var backupSet = BackupService.RunSingle(
                    t.OutputDir,
                    t.SourceInstanceName,
                    t.SourceInstanceConfig,
                    t.SourceDatabaseName
                );

                swBackup.Stop();
                steps[StepKeys.Backup] = swBackup.Elapsed;

                report(AfterBackup);

                // ----------------------
                // RESTORE
                // ----------------------
                setStep(StepKeys.ToLabel(StepKeys.Restore));
                var swRestore = Stopwatch.StartNew();

                RestoreService.RunSingle(
                    t.TargetInstanceName,
                    t.TargetInstanceConfig,
                    t.TargetDatabaseName,
                    backupSet.DumpPath
                );

                swRestore.Stop();
                steps[StepKeys.Restore] = swRestore.Elapsed;

                report(AfterRestore);
            },
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

                    StepDurations = new Dictionary<string, TimeSpan>(steps, StringComparer.OrdinalIgnoreCase),
                    SkippedSteps = new HashSet<string>(skipped, StringComparer.OrdinalIgnoreCase),

                    // Migration-specific
                    SourceInstance = t.SourceInstanceName,
                    SourceDatabase = t.SourceDatabaseName
                });
            },
            (t, ex, duration) =>
            {
                result.Failures.Add(new MigrationFailure
                {
                    // PgTaskResult = TARGET
                    Instance = t.TargetInstanceName,
                    Database = t.TargetDatabaseName,
                    Duration = duration,

                    StepDurations = new Dictionary<string, TimeSpan>(steps, StringComparer.OrdinalIgnoreCase),
                    SkippedSteps = new HashSet<string>(skipped, StringComparer.OrdinalIgnoreCase),

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