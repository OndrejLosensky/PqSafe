using PgSafe.Config;
using PgSafe.Cli.Runners;
using PgSafe.Cli.Renderers;
using PgSafe.Models.Migration;
using PgSafe.Services;
using Spectre.Console;
using PgSafe.Cli.Selectors;
using PgSafe.Utils;
using System.Diagnostics;
using PgSafe.Cli.Common;
using PgSafe.Enums;

namespace PgSafe.Cli.Menu;

public static class RunMigration
{
    public static void Start()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]PgSafe — Database Migration[/]");
        AnsiConsole.WriteLine();

        var config = ConfigLoaderUi.LoadOrShowError("pgsafe.yml");
        if (config is null)
            return;

        if (config.DryRun)
        {
            config.DryRun = DryRunSelector.Ask();

            AnsiConsole.MarkupLine(
                config.DryRun
                    ? "[bold yellow]PgSafe — Migration (DRY RUN)[/]"
                    : "[bold green]PgSafe — Migration[/]"
            );
        }

        // --- Source instance & database ---
        var sourceInstances = InstanceSelector.SelectInstances(config);
        if (sourceInstances.Count == 0)
            return;

        var sourceInstanceName = sourceInstances.First();
        var sourceInstance = config.Instances[sourceInstanceName];

        var sourceDbSelection = DatabaseSelector.SelectDatabases(
            config,
            new List<string> { sourceInstanceName },
            singleOnly: true
        );

        if (sourceDbSelection.Count == 0)
            return;

        var (_, sourceDatabaseName) = sourceDbSelection.First();

        // --- Target instance ---
        var targetInstances = InstanceSelector.SelectInstances(config);
        if (targetInstances.Count == 0)
            return;

        var targetInstanceName = targetInstances.First();
        var targetInstance = config.Instances[targetInstanceName];

        // --- Target database selection / creation ---
        var targetDbSelection = DatabaseSelector.SelectDatabases(
            config,
            new List<string> { targetInstanceName },
            singleOnly: true
        );

        string targetDatabaseName;

        if (targetDbSelection.Count == 0)
        {
            // No database found → ask for new name
            targetDatabaseName = NewDatabaseNamePrompt.Ask();
        }
        else
        {
            // Ask whether to use existing or create new
            var restoreTargetMode = RestoreTargetSelector.Ask();

            if (restoreTargetMode == RestoreTargetMode.ExistingDatabase)
            {
                (_, targetDatabaseName) = targetDbSelection.First();
            }
            else
            {
                targetDatabaseName = NewDatabaseNamePrompt.Ask();
            }
        }

        // --- Confirm migration and optionally create safety backup ---
        var confirmed = MigrationConfirmation.Ask(
            sourceInstanceName,
            sourceDatabaseName,
            targetInstanceName,
            targetDatabaseName,
            out var createSafetyBackup
        );

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Migration cancelled.[/]");
            return;
        }

        // --- Safety backup if requested ---
        if (createSafetyBackup)
        {
            if (!RunSafetyBackup(config, targetInstanceName, targetInstance, targetDatabaseName))
                return;
        }

        // --- Migration ---
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Starting migration…[/]");
        AnsiConsole.WriteLine();

        var swTotal = Stopwatch.StartNew();

        var migrationResult = MigrationProgressRunner.Run(
            config,
            sourceInstanceName,
            sourceDatabaseName,
            targetInstanceName,
            targetDatabaseName
        );

        swTotal.Stop();

        RunSummaryRenderer.Render(
            migrationResult.Successes,
            migrationResult.Failures,
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
        try
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Creating safety backup of '{instanceName}/{databaseName}'…[/]"
            );

            BackupService.RunSingle(
                config.OutputDir,
                instanceName,
                instance,
                databaseName
            );

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[red]Safety backup failed:[/] {ex.Message}"
            );

            return false;
        }
    }
}