using PgSafe.Config;
using PgSafe.Cli.Runners;
using PgSafe.Cli.Renderers;
using PgSafe.Models.Backup;
using PgSafe.Services;
using Spectre.Console;
using PgSafe.Cli.Selectors;
using PgSafe.Utils;
using System.Diagnostics;
using PgSafe.Cli.Common;
using PgSafe.Enums;

namespace PgSafe.Cli.Menu;

public static class RunRestore
{
    public static void Start()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]PgSafe — Restore[/]");
        AnsiConsole.WriteLine();

        var config = ConfigLoaderUi.LoadOrShowError("pgsafe.yml");
        if (config is null)
            return;
        
        AutoDiscoveryApplier.Apply(config);
        
        if (config.DryRun)
        {
            config.DryRun = DryRunSelector.Ask();

            AnsiConsole.MarkupLine(
                config.DryRun
                    ? "[bold yellow]PgSafe — Backup (DRY RUN)[/]"
                    : "[bold green]PgSafe — Backup[/]"
            );
        }


        // Instance selection
        var instances = InstanceSelector.SelectInstances(config);
        if (instances.Count == 0)
            return;

        var instanceName = instances.First();
        var instance = config.Instances[instanceName];

        // Database selection (single)
        var dbSelection = DatabaseSelector.SelectDatabases(
            config,
            new List<string> { instanceName },
            singleOnly: true
        );

        if (dbSelection.Count == 0)
            return;

        var (_, databaseName) = dbSelection.First();

        // Dump selection
        var dumpFile = DumpFileSelector.SelectDumpFile(
            config,
            instanceName,
            databaseName
        );

        if (dumpFile == null)
            return;

        var confirmed = RestoreConfirmation.Ask(
            instanceName,
            databaseName,
            dumpFile,
            out var createSafetyBackup
        );

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Restore cancelled.[/]");
            return;
        }
        
        var restoreTargetMode = RestoreTargetSelector.Ask();

        string targetDatabaseName;

        if (restoreTargetMode == RestoreTargetMode.ExistingDatabase)
        {
            targetDatabaseName = databaseName;
        }
        else
        {
            targetDatabaseName = NewDatabaseNamePrompt.Ask();
        }

        // SAFETY BACKUP
        if (createSafetyBackup)
        {
            if (!RunSafetyBackup(
                    config,
                    instanceName,
                    instance,
                    databaseName
                ))
                return;
        }
        
        if (restoreTargetMode == RestoreTargetMode.NewDatabase)
        {
            AnsiConsole.MarkupLine(
                $"[green]Creating database '{targetDatabaseName}'…[/]"
            );

            DatabaseProvisioningService.CreateDatabase(
                instance,
                targetDatabaseName
            );
        }

        // RESTORE
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Starting restore…[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine(
            targetDatabaseName == databaseName
                ? $"[yellow]Restoring into existing database:[/] [bold]{databaseName}[/]"
                : $"[green]Restoring into NEW database:[/] [bold]{targetDatabaseName}[/]"
        );
        AnsiConsole.WriteLine();
        
        // Measure total wall-clock time for the restore run
        var swTotal = Stopwatch.StartNew();

        var restoreResult = RestoreProgressRunner.Run(
            instanceName,
            instance,
            targetDatabaseName,
            dumpFile
        );

        swTotal.Stop();
        var totalElapsed = swTotal.Elapsed; // total duration for all tasks

        // Pass totalElapsed to the summary renderer
        RunSummaryRenderer.Render(
            restoreResult.Successes,
            restoreResult.Failures,
            totalElapsed
        );
    }
    
    private static bool RunSafetyBackup(
        PgSafeConfig config,
        string instanceName,
        PgInstanceConfig instance,
        string databaseName
    )
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Creating safety backup…[/]");
        AnsiConsole.WriteLine();

        var result = new BackupRunResult();
        var swTotal = Stopwatch.StartNew();

        try
        {
            var sw = Stopwatch.StartNew();

            var file = BackupService.RunSingle(
                config.OutputDir,
                instanceName,
                instance,
                databaseName
            );

            sw.Stop();

            result.Successes.Add(
                new BackupSuccess
                {
                    Instance = instanceName,
                    Database = databaseName,
                    FilePath = file,
                    FileSizeBytes = FileUtils.GetFileSize(file),
                    Duration = sw.Elapsed
                }
            );
        }
        catch (Exception ex)
        {
            result.Failures.Add(
                new BackupFailure
                {
                    Instance = instanceName,
                    Database = databaseName,
                    Error = ex.Message,
                    Duration = TimeSpan.Zero
                }
            );

            swTotal.Stop();

            RunSummaryRenderer.Render(
                result.Successes,
                result.Failures,
                swTotal.Elapsed
            );

            AnsiConsole.MarkupLine(
                "[red]Safety backup failed. Restore aborted.[/]"
            );

            return false;
        }

        swTotal.Stop();

        RunSummaryRenderer.Render(
            result.Successes,
            result.Failures,
            swTotal.Elapsed
        );

        return true;
    }
}
