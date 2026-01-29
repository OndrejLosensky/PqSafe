using PgSafe.Config;
using Spectre.Console;

namespace PgSafe.Cli.Selectors;

public static class InstanceSelector
{
    public static List<string> SelectInstances(
        PgSafeConfig config,
        string title = "Select PostgreSQL instances"
    )
    {
        var instanceNames = config.Instances.Keys.ToList();
        
        if (instanceNames.Count == 0)
            return [];

        if (instanceNames.Count == 1)
            return instanceNames;

        return AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title(title)
                .NotRequired()
                .Required()
                .AddChoices(instanceNames)
        );
    }
}