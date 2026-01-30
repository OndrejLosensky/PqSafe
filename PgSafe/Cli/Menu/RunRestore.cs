using PgSafe.Config;
using PgSafe.Services;
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

        var result = new RestoreRunResult();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Starting restore…[/]");
        AnsiConsole.WriteLine();

        // Progress
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
                        new RestoreSuccess(instanceName, databaseName, dumpFile)
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

        RenderSummary(result);

        AnsiConsole.MarkupLine("\n[grey]Press any key to continue…[/]");
        Console.ReadKey(true);
    }

    private static void RenderSummary(RestoreRunResult result)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Restore summary[/]");

        var table = new Table()
            .AddColumn("Instance")
            .AddColumn("Database")
            .AddColumn("Result");

        foreach (var success in result.Successes)
        {
            table.AddRow(
                success.Instance,
                success.Database,
                "[green]OK[/]"
            );
        }

        foreach (var failure in result.Failures)
        {
            table.AddRow(
                failure.Instance,
                failure.Database,
                "[red]FAILED[/]"
            );
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[green]Successful:[/] {result.Successes.Count}   " +
            $"[red]Failed:[/] {result.Failures.Count}"
        );
    }
}
