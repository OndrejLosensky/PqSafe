using System.Diagnostics;
using PgSafe.Models.Restore;
using PgSafe.Services;
using Spectre.Console;
using PgSafe.Config;
using PgSafe.Utils;

namespace PgSafe.Cli.Runners;

public static class RestoreProgressRunner
{
    public static RestoreRunResult Run(
        string instanceName,
        PgInstanceConfig instance,
        string databaseName,
        string dumpFile
    )
    {
        var result = new RestoreRunResult();

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
                var task = ctx.AddTask(
                    $"{instanceName}/{databaseName}",
                    maxValue: 100
                );

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    RestoreService.RunSingle(
                        instanceName,
                        instance,
                        databaseName,
                        dumpFile
                    );

                    stopwatch.Stop();
                    task.Value = 100;

                    result.Successes.Add(new RestoreSuccess
                    {
                        Instance = instanceName,
                        Database = databaseName,
                        FilePath = dumpFile,
                        FileSizeBytes = FileUtils.GetFileSize(dumpFile),
                        Duration = stopwatch.Elapsed
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    task.StopTask();

                    result.Failures.Add(new RestoreFailure
                    {
                        Instance = instanceName,
                        Database = databaseName,
                        Error = ex.Message,
                        Duration = stopwatch.Elapsed
                    });
                }
            });

        return result;
    }
}
