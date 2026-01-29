using Spectre.Console;

namespace PgSafe.Cli.Menu;

public static class Menu
{
    public static void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();

            AnsiConsole.Write(
                new FigletText("PgSafe")
                    .Centered()
                    .Color(Color.Green)
            );

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What do you want to do?[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Green))
                    .AddChoices(
                        "Backup databases",
                        "Restore databases",
                        "Exit"
                    )
            );

            switch (choice)
            {
                case "Backup databases":
                    ShowBackup();
                    break;

                case "Restore databases":
                    RunRestore();
                    break;

                case "Exit":
                    AnsiConsole.MarkupLine("[grey]Goodbye 👋[/]");
                    return;
            }
        }
    }

    private static void ShowBackup()
    {
        RunBackup.Start(); 
        Pause();
    }

    private static void RunRestore()
    {
        AnsiConsole.MarkupLine("[yellow]Restore flow not implemented yet.[/]");
        Pause();
    }

    private static void Pause()
    {
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}