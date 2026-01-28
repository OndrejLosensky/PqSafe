using System.CommandLine;
using PgSafe.Config;

namespace PgSafe.Cli;

public static class BackupCommand
{
    public static Command Create()
    {
        var command = new Command("backup", "Create backups for configured databases");

        var configOption = new Option<string>(
            name: "--config",
            description: "Path to pgsafe.yml",
            getDefaultValue: () => "pgsafe.yml"
        );

        command.AddOption(configOption);

        command.SetHandler((string configPath) =>
        {
            Console.WriteLine("PgSafe backup started (v0.1)");

            var config = ConfigLoader.Load(configPath);

            Console.WriteLine($"Output dir: {config.OutputDir}");
            Console.WriteLine($"Databases configured: {config.Databases.Count}");

            foreach (var db in config.Databases)
            {
                Console.WriteLine(
                    $"- {db.Name} ({db.Database}) @ {db.Host}:{db.Port}"
                );
            }

            Console.WriteLine("Config loaded successfully ✔");
        }, configOption);

        return command;
    }
}