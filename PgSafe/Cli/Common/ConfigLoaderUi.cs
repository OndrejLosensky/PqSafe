using PgSafe.Config;
using Spectre.Console;

namespace PgSafe.Cli.Common;

public static class ConfigLoaderUi
{
    public static PgSafeConfig? LoadOrShowError(string path)
    {
        try
        {
            return ConfigLoader.Load(path);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[red]Failed to load config:[/] {ex.Message}"
            );
            return null;
        }
    }
}