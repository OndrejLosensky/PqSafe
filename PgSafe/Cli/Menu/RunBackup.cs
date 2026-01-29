using PgSafe.Config;
using PgSafe.Services;
using PgSafe.Models;
using Spectre.Console;

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

        var selections = SelectDatabases(config);

        if (selections.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No databases selected. Aborting.[/]");
            return;
        }

        ApplySelection(config, selections);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Starting backup…[/]");
        AnsiConsole.WriteLine();

        var result = BackupService.Run(config);

        RenderSummary(result);

        AnsiConsole.MarkupLine("\n[grey]Press any key to continue…[/]");
        Console.ReadKey(true);
    }

    private static List<(string instance, string database)> SelectDatabases(PgSafeConfig config)
    {
        var allDbs = new List<(string instance, string database)>();

        foreach (var (instanceName, instance) in config.Instances)
        {
            foreach (var dbName in instance.Databases.Keys)
            {
                allDbs.Add((instanceName, dbName));
            }
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What do you want to back up?")
                .AddChoices(
                    "All databases",
                    "Select databases"
                )
        );

        if (choice == "All databases")
            return allDbs;

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<(string instance, string database)>()
                .Title("Select databases to back up")
                .NotRequired()
                .UseConverter(x => $"{x.instance}/{x.database}")
                .AddChoices(allDbs)
        );

        return selected.ToList();
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
                $"[red]FAILED[/]"
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
