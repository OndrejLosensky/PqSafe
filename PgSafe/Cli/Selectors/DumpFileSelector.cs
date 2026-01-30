using Spectre.Console;
using PgSafe.Config;

namespace PgSafe.Cli.Selectors;

public static class DumpFileSelector
{
    private const string BackItem = "< Back";

    public static string? SelectDumpFile(
        PgSafeConfig config,
        string instance,
        string database
    )
    {
        var baseDir = Path.Combine(
            config.OutputDir,
            instance,
            database
        );

        if (!Directory.Exists(baseDir))
        {
            AnsiConsole.MarkupLine(
                $"[red]No backups found for {instance}/{database}[/]"
            );
            return null;
        }

        var dumps = Directory
            .GetFiles(baseDir, "*.dump")
            .OrderByDescending(Path.GetFileName)
            .ToList();

        if (dumps.Count == 0)
        {
            AnsiConsole.MarkupLine(
                $"[red]No dump files found for {instance}/{database}[/]"
            );
            return null;
        }

        // Single dump → no prompt
        if (dumps.Count == 1)
            return dumps[0];

        var choices = new List<string> { BackItem };
        choices.AddRange(
            dumps.Select(Path.GetFileName!)
        );

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select dump for [green]{instance}/{database}[/]")
                .PageSize(10)
                .AddChoices(choices)
        );

        if (selected == BackItem)
            return null;

        return Path.Combine(baseDir, selected);
    }
}