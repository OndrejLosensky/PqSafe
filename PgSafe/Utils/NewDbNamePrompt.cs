using Spectre.Console;

namespace PgSafe.Utils;

public static class NewDatabaseNamePrompt
{
    public static string Ask()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Enter new database name:")
                .Validate(name =>
                    string.IsNullOrWhiteSpace(name)
                        ? ValidationResult.Error("Database name cannot be empty")
                        : ValidationResult.Success()
                )
        );
    }
}
