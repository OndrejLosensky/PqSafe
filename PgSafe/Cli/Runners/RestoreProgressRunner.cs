using PgSafe.Models.Restore;
using PgSafe.Services;
using Spectre.Console;
using PgSafe.Config;

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

                try
                {
                    RestoreService.RunSingle(
                        instanceName,
                        instance,
                        databaseName,
                        dumpFile
                    );

                    task.Value = 100;

                    result.Successes.Add(
                        new RestoreSuccess(
                            instanceName,
                            databaseName,
                            dumpFile
                        )
                    );
                }
                catch (Exception ex)
                {
                    task.StopTask();

                    result.Failures.Add(
                        new RestoreFailure(
                            instanceName,
                            databaseName,
                            ex.Message
                        )
                    );
                }
            });

        return result;
    }
}