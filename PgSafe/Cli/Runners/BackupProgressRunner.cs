using PgSafe.Config;
using PgSafe.Models.Backup;
using PgSafe.Services;
using PgSafe.Utils;
using Spectre.Console;
using System.Diagnostics;

namespace PgSafe.Cli.Runners;

public static class BackupProgressRunner
{
    // FULL RUN 
    public static BackupRunResult Run(PgSafeConfig config)
    {
        var targets = new List<BackupTarget>();

        foreach (var (instanceName, instance) in config.Instances)
        {
            foreach (var (dbName, db) in instance.Databases)
            {
                if (!db.Backup.Enabled)
                    continue;

                targets.Add(
                    new BackupTarget(
                        instanceName,
                        instance,
                        dbName
                    )
                );
            }
        }

        return RunTargets(config, targets);
    }

    // SAFETY BACKUP / SINGLE DB RUN
    public static BackupRunResult RunSingle(
        PgSafeConfig config,
        string instanceName,
        PgInstanceConfig instance,
        string databaseName
    )
    {
        var targets = new List<BackupTarget>
        {
            new(
                instanceName,
                instance,
                databaseName
            )
        };

        return RunTargets(config, targets);
    }

    // SHARED EXECUTION CORE
    private static BackupRunResult RunTargets(
        PgSafeConfig config,
        List<BackupTarget> targets
    )
    {
        var result = new BackupRunResult();

        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .Start(ctx =>
            {
                foreach (var target in targets)
                {
                    var task = ctx.AddTask(
                        $"{target.InstanceName}/{target.DatabaseName}",
                        maxValue: 100
                    );

                    var sw = Stopwatch.StartNew();

                    try
                    {
                        var file = BackupService.RunSingle(
                            config.OutputDir,
                            target.InstanceName,
                            target.InstanceConfig,
                            target.DatabaseName
                        );

                        sw.Stop();
                        task.Value = 100;

                        result.Successes.Add(
                            new BackupSuccess
                            {
                                Instance = target.InstanceName,
                                Database = target.DatabaseName,
                                FilePath = file,
                                FileSizeBytes = FileUtils.GetFileSize(file),
                                Duration = sw.Elapsed
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        task.StopTask();

                        result.Failures.Add(
                            new BackupFailure
                            {
                                Instance = target.InstanceName,
                                Database = target.DatabaseName,
                                Error = ex.Message,
                                Duration = sw.Elapsed
                            }
                        );
                    }
                }
            });

        return result;
    }
}
