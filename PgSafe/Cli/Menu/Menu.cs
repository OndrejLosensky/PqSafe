using System.Reflection;
using Spectre.Console;

namespace PgSafe.Cli.Menu;

public static class Menu
{
    public static void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();

            var version = Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version?
                .ToString(3);

            AnsiConsole.Write(
                new FigletText("PgSafe")
                    .LeftJustified()
                    .Color(Color.Green)
            );

            AnsiConsole.MarkupLine($"[grey]v{version}[/]");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What do you want to do?[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Green))
                    .AddChoices(
                        "Backup databases",
                        "Restore databases",
                        "Migrate databases",
                        "Exit"
                    )
            );

            switch (choice)
            {
                case "Backup databases":
                    ShowBackup();
                    break;

                case "Restore databases":
                    ShowRestore();
                    break;
                
                case "Migrate databases":
                    ShowMigrate();
                    break;

                case "Exit":
                    AnsiConsole.MarkupLine("[grey]Goodbye [/]");
                    return;
            }
        }
    }

    private static void ShowBackup()
    {
        RunBackup.Start(); 
        Pause();
    }

    private static void ShowRestore()
    {
        RunRestore.Start(); 
        Pause();
    }
    
    private static void ShowMigrate()
    {
        RunMigration.Start();
        Pause();
    }

    private static void Pause()
    {
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}