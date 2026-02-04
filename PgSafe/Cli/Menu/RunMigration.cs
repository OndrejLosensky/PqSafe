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

        // --- Target instance & database ---
        var targetInstances = InstanceSelector.SelectInstances(config);
        if (targetInstances.Count == 0)
            return;

        var targetInstanceName = targetInstances.First();
        var targetInstance = config.Instances[targetInstanceName];

        var targetDbSelection = DatabaseSelector.SelectDatabases(
            config,
            new List<string> { targetInstanceName },
            singleOnly: true
        );

        if (targetDbSelection.Count == 0)
            return;

        var (_, targetDatabaseName) = targetDbSelection.First();

        // Confirm migration
        var confirmed = MigrationConfirmation.Ask(
            sourceInstanceName,
            sourceDatabaseName,
            targetInstanceName,
            targetDatabaseName
        );

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Migration cancelled.[/]");
            return;
        }

        // Create target DB if needed
        if (!DatabaseUtils.DatabaseExists(targetInstance, targetDatabaseName))
        {
            AnsiConsole.MarkupLine($"[green]Creating target database '{targetDatabaseName}'…[/]");
            DatabaseProvisioningService.CreateDatabase(targetInstance, targetDatabaseName);
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
}
