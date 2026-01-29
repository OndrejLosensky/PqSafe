using PgSafe.Config;
using Spectre.Console;

namespace PgSafe.Cli.Selectors;

public static class DatabaseSelector
{
    public static List<(string instance, string database)> SelectDatabases(
        PgSafeConfig config,
        List<string> selectedInstances
    )
    {
        var allDbs = new List<(string instance, string database)>();
        var backItem = ("__BACK__", "__BACK__");

        
        foreach (var instanceName in selectedInstances)
        {
            if (!config.Instances.TryGetValue(instanceName, out var instance))
                continue;

            foreach (var dbName in instance.Databases.Keys)
            {
                allDbs.Add((instanceName, dbName));
            }
        }

        if (allDbs.Count == 0)
            return [];

        // If there's only one DB, don't ask questions
        if (allDbs.Count == 1)
            return allDbs;

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What do you want to back up?")
                .AddChoices(
                    "All databases",
                    "Select databases"
                )
        );

        if (choice == "All databases")
            return allDbs;

        return AnsiConsole.Prompt(
            new MultiSelectionPrompt<(string instance, string database)>()
                .Title("Select databases")
                .NotRequired()
                .UseConverter(x => $"{x.instance}/{x.database}")
                .AddChoices(allDbs)
        ).ToList();
    }
}