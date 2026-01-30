using PgSafe.Models;
using PgSafe.Utils;
using Spectre.Console;

namespace PgSafe.Cli.Runners;

public static class RunSummaryRenderer
{
    public static void Render<TSuccess, TFailure>(
        IReadOnlyList<TSuccess> successes,
        IReadOnlyList<TFailure> failures
    )
        where TSuccess : PgTaskResult
        where TFailure : PgTaskResult
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Summary[/]");

        var showFile = successes.Any(s => s.FilePath != null);

        var table = new Table()
            .AddColumn("Instance")
            .AddColumn("Database");

        if (showFile)
            table.AddColumn("File");

        table
            .AddColumn("Duration")
            .AddColumn("Result");

        foreach (var s in successes)
        {
            var row = new List<string>
            {
                s.Instance,
                s.Database
            };

            if (showFile)
                row.Add(Path.GetFileName(s.FilePath!));

            row.Add(TimeFormatter.Humanize(s.Duration));
            row.Add("[green]OK[/]");

            table.AddRow(row.ToArray());
        }

        foreach (var f in failures)
        {
            var row = new List<string>
            {
                f.Instance,
                f.Database
            };

            if (showFile)
                row.Add("-");

            row.Add(TimeFormatter.Humanize(f.Duration));
            row.Add("[red]FAILED[/]");

            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine(
            $"\n[green]Successful:[/] {successes.Count}   " +
            $"[red]Failed:[/] {failures.Count}"
        );
    }
}