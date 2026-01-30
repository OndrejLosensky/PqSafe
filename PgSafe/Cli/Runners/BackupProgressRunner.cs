using PgSafe.Config;
using PgSafe.Models.Backup;
using PgSafe.Services;
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

                        try
                        {
                            BackupService.RunSingle(
                                config.OutputDir,
                                instanceName,
                                instance,
                                dbName
                            );

                            task.Value = 100;

                            result.Successes.Add(
                                new BackupSuccess(instanceName, dbName, "OK")
                            );
                        }
                        catch (Exception ex)
                        {
                            task.StopTask();

                            result.Failures.Add(
                                new BackupFailure(
                                    instanceName,
                                    dbName,
                                    ex.Message
                                )
                            );
                        }
                    }
                }
            });

        return result;
    }
}
