using Spectre.Console;

namespace PgSafe.Cli.Selectors;

public static class DryRunSelector
{
    public static bool Ask()
    {
        AnsiConsole.WriteLine();

        return AnsiConsole.Confirm(
            "[yellow]Run in dry-run mode? (no changes will be made)[/]",
            defaultValue: false
        );
    }
}