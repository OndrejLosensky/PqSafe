using System.CommandLine;

namespace PgSafe.Cli;

public static class BackupCommand
{
    public static Command Create()
    {
        var command = new Command("backup", "Create backups for configured databases");

        command.SetHandler(() =>
        {
            Console.WriteLine("PgSafe backup started (v0.1)");
            Console.WriteLine("This is a stub implementation.");
        });

        return command;
    }
}