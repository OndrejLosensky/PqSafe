using Spectre.Console;
using PgSafe.Config;
using PgSafe.Models.Backup;
using PgSafe.Services;
using System.Globalization;

namespace PgSafe.Cli.Selectors;

public static class DumpFileSelector
{
    private const string BackItem = "< Back>";

    public static BackupSet? SelectDumpFile(
        PgSafeConfig config,
        string instance,
        string database
    )
    {
        var repo = new BackupRepositoryService(config.OutputDir);

        // Get all backups, sorted newest first
        var backups = repo.GetAllBackups(instance, database)
                          .OrderByDescending(b =>
                          {
                              if (DateTime.TryParseExact(
                                  b.BackupId,
                                  "yyyy-MM-dd_HH-mm-ss",
                                  CultureInfo.InvariantCulture,
                                  DateTimeStyles.None,
                                  out var dt))
                                  return dt;

                              return DateTime.MinValue;
                          })
                          .ToList();

        if (backups.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No backups found for {instance}/{database}[/]");
            return null;
        }

        // Build menu choices
        var choices = new List<string> { BackItem };
        choices.AddRange(backups.Select(b =>
        {
            // Format size
            var sizeKb = b.Metadata.SizeBytes / 1024.0;
            string sizeStr = sizeKb > 1024
                ? $"{sizeKb / 1024:F1} MB"
                : $"{sizeKb:F0} KB";

            // Format timestamp nicely
            string timestamp = b.BackupId;
            if (DateTime.TryParseExact(
                b.BackupId,
                "yyyy-MM-dd_HH-mm-ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            {
                timestamp = dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // PG version
            string pgVersion = string.IsNullOrWhiteSpace(b.Metadata.PgVersion)
                ? "unknown"
                : b.Metadata.PgVersion;

            // Only show formatted timestamp, size, PG version
            return $"{timestamp} — {sizeStr} — {b.Metadata.TableCount} tables / {b.Metadata.RowCount} rows — PG {pgVersion}";
        }));

        // Prompt user to select a backup
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select backup for [green]{instance}/{database}[/]")
                .PageSize(10)
                .AddChoices(choices)
        );

        if (selected == BackItem)
            return null;

        // Map selected index back to BackupSet
        int selectedIndex = choices.IndexOf(selected) - 1; // -1 because of BackItem
        return backups[selectedIndex];
    }
}
