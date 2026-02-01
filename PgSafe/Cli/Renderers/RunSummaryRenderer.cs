using PgSafe.Models;
using PgSafe.Utils;
using Spectre.Console;
using System.Reflection;

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

        var showError = failures.Any(f => !string.IsNullOrWhiteSpace(TryGetErrorText(f)));

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

        if (showError)
            table.AddColumn("Error");

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

            if (showError)
                row.Add("-");

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

            if (showError)
            {
                var err = TryGetErrorText(f);
                row.Add(string.IsNullOrWhiteSpace(err) ? "-" : Markup.Escape(TrimOneLine(err!, 160)));
            }

            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);

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

    private static string? TryGetErrorText<T>(T obj)
    {
        var t = obj?.GetType();
        if (t is null) return null;

        var prop =
            t.GetProperty("Error", BindingFlags.Public | BindingFlags.Instance)
            ?? t.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance)
            ?? t.GetProperty("ErrorMessage", BindingFlags.Public | BindingFlags.Instance);

        return prop?.GetValue(obj) as string;
    }

    private static string TrimOneLine(string s, int maxLen)
    {
        var oneLine = s.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
        return oneLine.Length <= maxLen ? oneLine : oneLine.Substring(0, maxLen - 1) + "…";
    }
}