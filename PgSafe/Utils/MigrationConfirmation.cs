using Spectre.Console;

namespace PgSafe.Cli.Common;

public static class MigrationConfirmation
{
    public static bool Ask(
        string sourceInstance,
        string sourceDatabase,
        string targetInstance,
        string targetDatabase,
        out bool createSafetyBackup
    )
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            $"""
             [yellow]You are about to migrate a database[/]

             [bold]Source:[/] {sourceInstance}/{sourceDatabase}
             [bold]Target:[/] {targetInstance}/{targetDatabase}

             [red]⚠ This may overwrite data on the target database![/]
             """
        )
        {
            Header = new PanelHeader("[bold red]Confirm migration[/]", Justify.Center),
            Border = BoxBorder.Double
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        createSafetyBackup = AnsiConsole.Confirm(
            "Create a safety backup of the target database before migrating?",
            defaultValue: true
        );

        return AnsiConsole.Confirm(
            "[red]Do you really want to continue?[/]",
            defaultValue: false
        );
    }
}