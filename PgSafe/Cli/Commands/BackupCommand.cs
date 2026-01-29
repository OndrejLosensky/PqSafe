using System.CommandLine;
using PgSafe.Services;
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
            var config = ConfigLoader.Load(configPath);
            var result = BackupService.Run(config);

            Console.WriteLine();
            Console.WriteLine("Backup summary:");
            Console.WriteLine($"  Successful: {result.Successes.Count}");
            Console.WriteLine($"  Failed:     {result.Failures.Count}");

            if (result.HasFailures)
                Environment.ExitCode = 1;
        }, configOption);

        return command;
    }
}