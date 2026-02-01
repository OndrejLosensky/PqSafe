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

        if (config.DryRun)
        {
            config.DryRun = DryRunSelector.Ask();

            AnsiConsole.MarkupLine(
                config.DryRun
                    ? "[bold yellow]PgSafe — Restore (DRY RUN)[/]"
                    : "[bold green]PgSafe — Restore[/]"
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

        // Dump/backup selection
        var backupSet = DumpFileSelector.SelectDumpFile(
            config,
            instanceName,
            databaseName
        );

        if (backupSet == null)
            return;

        var confirmed = RestoreConfirmation.Ask(
            instanceName,
            databaseName,
            backupSet.DumpPath,
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
            if (!RunSafetyBackup(config, instanceName, instance, databaseName))
                return;
        }

        // Create new database if needed
        if (restoreTargetMode == RestoreTargetMode.NewDatabase)
        {
            AnsiConsole.MarkupLine($"[green]Creating database '{targetDatabaseName}'…[/]");
            DatabaseProvisioningService.CreateDatabase(instance, targetDatabaseName);
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

        var swTotal = Stopwatch.StartNew();

        var restoreResult = RestoreProgressRunner.Run(
            backupSet,
            instance,
            targetDatabaseName
        );

        swTotal.Stop();

        RunSummaryRenderer.Render(
            restoreResult.Successes,
            restoreResult.Failures,
            swTotal.Elapsed
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
            var backupSet = BackupService.RunSingle(
                config.OutputDir,
                instanceName,
                instance,
                databaseName
            );
            sw.Stop();

            result.Successes.Add(new BackupSuccess
            {
                Instance = instanceName,
                Database = databaseName,
                FilePath = backupSet.DumpPath,
                FileSizeBytes = FileUtils.GetFileSize(backupSet.DumpPath),
                Duration = sw.Elapsed
            });
        }
        catch (Exception ex)
        {
            result.Failures.Add(new BackupFailure
            {
                Instance = instanceName,
                Database = databaseName,
                Error = ex.Message,
                Duration = TimeSpan.Zero
            });

            swTotal.Stop();
            RunSummaryRenderer.Render(result.Successes, result.Failures, swTotal.Elapsed);

            AnsiConsole.MarkupLine("[red]Safety backup failed. Restore aborted.[/]");
            return false;
        }

        swTotal.Stop();
        RunSummaryRenderer.Render(result.Successes, result.Failures, swTotal.Elapsed);
        return true;
    }
}
