using PgSafe.Cli.Selectors;
using PgSafe.Cli.Common;
using PgSafe.Cli.Runners;
using PgSafe.Cli.Renderers;
using Spectre.Console;
using System.Diagnostics;

namespace PgSafe.Cli.Menu;  

public static class RunBackup
{
    public static void Start()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]PgSafe — Backup[/]\n");

        var config = ConfigLoaderUi.LoadOrShowError("pgsafe.yml");
        if (config is null)
            return;
        
        if (config.DryRun)
        {
            config.DryRun = DryRunSelector.Ask();

            AnsiConsole.MarkupLine(
                config.DryRun
                    ? "[bold yellow]PgSafe — Backup (DRY RUN)[/]"
                    : "[bold green]PgSafe — Backup[/]"
            );
        }

        var selectedInstances = InstanceSelector.SelectInstances(config);
        if (selectedInstances.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No instances selected. Aborting.[/]");
            return;
        }

        var selectedDatabases = DatabaseSelector.SelectDatabases(
            config,
            selectedInstances
        );

        if (selectedDatabases.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No databases selected. Aborting.[/]");
            return;
        }

        SelectionApplier.ApplyDatabaseSelection(config, selectedDatabases);

        AnsiConsole.MarkupLine("\n[green]Starting backup…[/]\n");

        // Measure total wall-clock time for the backup run
        var swTotal = Stopwatch.StartNew();

        var result = BackupProgressRunner.Run(config);

        swTotal.Stop();
        var totalElapsed = swTotal.Elapsed;

        RunSummaryRenderer.Render(
            result.Successes,
            result.Failures,
            totalElapsed
        );
    }
}