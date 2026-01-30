using PgSafe.Models;
using PgSafe.Utils;
using Spectre.Console;

namespace PgSafe.Cli.Renderers;

public static class RunSummaryRenderer
{
    public static void Render<TSuccess, TFailure>(
        IReadOnlyList<TSuccess> successes,
        IReadOnlyList<TFailure> failures,
        TimeSpan totalTime
    )
        where TSuccess : PgTaskResult
        where TFailure : PgTaskResult
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Summary[/]");

        var showFile = successes.Any(s => s.FilePath != null);
        var showSize = successes.Any(s => s.FileSizeBytes != null);

        var table = new Table()
            .AddColumn("Instance")
            .AddColumn("Database");

        if (showFile)
            table.AddColumn("File");

        if (showSize)
            table.AddColumn("Size");

        table
            .AddColumn("Duration")
            .AddColumn("Result");

        // Render successes
        foreach (var s in successes)
        {
            var row = new List<string>
            {
                s.Instance,
                s.Database
            };

            if (showFile)
                row.Add(Path.GetFileName(s.FilePath!));

            if (showSize)
                row.Add(
                    s.FileSizeBytes is not null
                        ? SizeFormatter.Humanize(s.FileSizeBytes.Value)
                        : "-"
                );

            row.Add(TimeFormatter.Humanize(s.Duration));
            row.Add("[green]OK[/]");

            table.AddRow(row.ToArray());
        }

        // Render failures
        foreach (var f in failures)
        {
            var row = new List<string>
            {
                f.Instance,
                f.Database
            };

            if (showFile)
                row.Add("-");

            if (showSize)
                row.Add("-");

            row.Add(TimeFormatter.Humanize(f.Duration));
            row.Add("[red]FAILED[/]");

            table.AddRow(row.ToArray());
        }

        // Show main table
        AnsiConsole.Write(table);

        // Totals table (uses WALL-CLOCK time)
        var totalTable = new Table()
            .AddColumn("[bold]Total Duration[/]")
            .AddColumn("Successful")
            .AddColumn("Failed");

        totalTable.AddRow(
            $"[bold]{TimeFormatter.Humanize(totalTime)}[/]",
            $"[green]{successes.Count}[/]",
            failures.Count > 0
                ? $"[red]{failures.Count}[/]"
                : failures.Count.ToString()
        );

        AnsiConsole.WriteLine();
        AnsiConsole.Write(totalTable);
    }
}
