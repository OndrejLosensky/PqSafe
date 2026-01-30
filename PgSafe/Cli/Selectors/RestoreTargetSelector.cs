using PgSafe.Enums;
using Spectre.Console;

namespace PgSafe.Cli.Selectors;

public static class RestoreTargetSelector
{
    public static RestoreTargetMode Ask()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<RestoreTargetMode>()
                .Title("How do you want to restore?")
                .AddChoices(
                    RestoreTargetMode.ExistingDatabase,
                    RestoreTargetMode.NewDatabase
                )
                .UseConverter(x =>
                    x == RestoreTargetMode.ExistingDatabase
                        ? "Restore into existing database"
                        : "Restore into NEW database"
                )
        );
    }
}
