using Spectre.Console;

namespace PgSafe.Utils;

public static class RestoreConfirmation
{
    public static bool Ask(
        string instance,
        string database,
        string dumpFile
    )
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            $"""
             [yellow]You are about to restore a database[/]

             [bold]Instance:[/] {instance}
             [bold]Database:[/] {database}
             [bold]Dump file:[/] {dumpFile}

             [red]⚠ This will overwrite existing data![/]
             """
        )
        {
            Header = new PanelHeader("[bold red]Confirm restore[/]", Justify.Center),
            Border = BoxBorder.Double
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        return AnsiConsole.Confirm(
            "[red]Do you really want to continue?[/]",
            defaultValue: false
        );
    }
}