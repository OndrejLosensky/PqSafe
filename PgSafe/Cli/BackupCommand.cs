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
            BackupService.Run(config);
        }, configOption);

        return command;
    }
}