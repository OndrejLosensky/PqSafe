using System.CommandLine;
using PgSafe.Cli;
using DotNetEnv;

Env.Load();

var rootCommand = new RootCommand("PgSafe - PostgreSQL backup & restore CLI");

rootCommand.AddCommand(BackupCommand.Create());

return await rootCommand.InvokeAsync(args);