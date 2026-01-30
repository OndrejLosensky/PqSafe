using System.Diagnostics;
using PgSafe.Config;
using PgSafe.Models.Backup;
using PgSafe.Services;
using PgSafe.Utils;
using Spectre.Console;

namespace PgSafe.Cli.Runners;

public static class BackupProgressRunner
{
    public static BackupRunResult Run(PgSafeConfig config)
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
                foreach (var (instanceName, instance) in config.Instances)
                {
                    foreach (var (dbName, db) in instance.Databases)
                    {
                        if (!db.Backup.Enabled)
                            continue;

                        var task = ctx.AddTask(
                            $"{instanceName}/{dbName}",
                            maxValue: 100
                        );

                        var sw = Stopwatch.StartNew();

                        try
                        {
                            var filePath = BackupService.RunSingle(
                                config.OutputDir,
                                instanceName,
                                instance,
                                dbName
                            );

                            sw.Stop();
                            task.Value = 100;

                            result.Successes.Add(new BackupSuccess
                            {
                                Instance = instanceName,
                                Database = dbName,
                                FilePath = filePath,
                                FileSizeBytes = FileUtils.GetFileSize(filePath),
                                Duration = sw.Elapsed
                            });
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            task.StopTask();

                            result.Failures.Add(new BackupFailure
                            {
                                Instance = instanceName,
                                Database = dbName,
                                Error = ex.Message,
                                Duration = sw.Elapsed
                            });
                        }
                    }
                }
            });

        return result;
    }
}
