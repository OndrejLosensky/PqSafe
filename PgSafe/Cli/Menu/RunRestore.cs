using PgSafe.Cli.Renderers;
using PgSafe.Config;
using PgSafe.Cli.Runners;
using PgSafe.Models.Restore;
using Spectre.Console;
using PgSafe.Cli.Selectors;
using PgSafe.Utils;

namespace PgSafe.Cli.Menu;

public static class RunRestore
{
    public static void Start()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]PgSafe — Restore[/]");
        AnsiConsole.WriteLine();

        PgSafeConfig config;

        try
        {
            config = ConfigLoader.Load("pgsafe.yml");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load config:[/] {ex.Message}");
            return;
        }

        // Instance selection
        var instances = InstanceSelector.SelectInstances(config);
        if (instances.Count == 0)
            return;

        // Restore is one DB at a time → force single instance
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
            dumpFile
        );

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Restore cancelled.[/]");
            AnsiConsole.MarkupLine("[grey]Press any key to continue…[/]");
            Console.ReadKey(true);
            return;
        }

        // Progress
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Starting restore…[/]");
        AnsiConsole.WriteLine();

        var result = RestoreProgressRunner.Run(
            instanceName,
            instance,
            databaseName,
            dumpFile
        );

        RunSummaryRenderer.Render(
            "Restore summary",
            result.Successes.Select(s => (s.Instance, s.Database)),
            result.Failures.Select(f => (f.Instance, f.Database, f.Error))
        );
    }
}
