using PgSafe.Config;
using PgSafe.Services;
using PgSafe.Models;
using Spectre.Console;
using PgSafe.Cli.Selectors;

namespace PgSafe.Cli.Menu;

public static class RunBackup
{
    public static void Start()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]PgSafe — Backup[/]");
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

        // Select instances
        var selectedInstances = InstanceSelector.SelectInstances(config);

        if (selectedInstances.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No instances selected. Aborting.[/]");
            return;
        }

        // Select databases (scoped to instances)
        var selectedDatabases = DatabaseSelector.SelectDatabases(
            config,
            selectedInstances
        );

        if (selectedDatabases.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No databases selected. Aborting.[/]");
            return;
        }

        // Apply selection to config
        ApplySelection(config, selectedDatabases);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Starting backup…[/]");
        AnsiConsole.WriteLine();
        
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

                        var task = ctx.AddTask($"{instanceName}/{dbName}", maxValue: 100);

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
                                new BackupFailure(instanceName, dbName, ex.Message)
                            );
                        }
                    }
                }
            });

        // Render summary 
        RenderSummary(result);

        AnsiConsole.MarkupLine("\n[grey]Press any key to continue…[/]");
        Console.ReadKey(true);
    }

    private static void ApplySelection(
        PgSafeConfig config,
        List<(string instance, string database)> selected
    )
    {
        var selectedSet = selected.ToHashSet();

        foreach (var (instanceName, instance) in config.Instances)
        {
            foreach (var (dbName, db) in instance.Databases)
            {
                db.Backup.Enabled = selectedSet.Contains((instanceName, dbName));
            }
        }
    }

    private static void RenderSummary(BackupRunResult result)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Backup summary[/]");

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
