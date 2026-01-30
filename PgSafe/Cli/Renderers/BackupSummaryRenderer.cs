using PgSafe.Models.Backup;
using Spectre.Console;

namespace PgSafe.Cli.Renderers;

public static class BackupSummaryRenderer
{
    public static void Render(BackupRunResult result)
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

        AnsiConsole.MarkupLine(
            $"\n[green]Successful:[/] {result.Successes.Count}   " +
            $"[red]Failed:[/] {result.Failures.Count}"
        );
    }
}