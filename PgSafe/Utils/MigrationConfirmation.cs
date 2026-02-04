using Spectre.Console;

namespace PgSafe.Cli.Common;

public static class MigrationConfirmation
{
    public static bool Ask(
        string sourceInstance,
        string sourceDatabase,
        string targetInstance,
        string targetDatabase
    )
    {
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm(
            $"Are you sure you want to migrate [bold]{sourceInstance}/{sourceDatabase}[/] " +
            $"to [bold]{targetInstance}/{targetDatabase}[/]?"
        );
    }
}