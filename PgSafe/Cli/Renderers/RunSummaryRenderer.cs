using Spectre.Console;

namespace PgSafe.Cli.Renderers;

public static class RunSummaryRenderer
{
    public static void Render(
        string title,
        IEnumerable<(string Instance, string Database)> successes,
        IEnumerable<(string Instance, string Database, string Error)> failures
    )
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]{title}[/]");

        var table = new Table()
            .AddColumn("Instance")
            .AddColumn("Database")
            .AddColumn("Result");

        foreach (var s in successes)
        {
            table.AddRow(
                s.Instance,
                s.Database,
                "[green]OK[/]"
            );
        }

        foreach (var f in failures)
        {
            table.AddRow(
                f.Instance,
                f.Database,
                "[red]FAILED[/]"
            );
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[green]Successful:[/] {successes.Count()}   " +
            $"[red]Failed:[/] {failures.Count()}"
        );
    }
}