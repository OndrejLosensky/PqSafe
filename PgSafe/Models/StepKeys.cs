namespace PgSafe.Models;

public static class StepKeys
{
    public const string EnsureDb = "ensure-db";
    public const string Backup = "backup";
    public const string Restore = "restore";

    public static string ToLabel(string key) => key switch
    {
        EnsureDb => "Ensure DB",
        Backup => "Backup",
        Restore => "Restore",
        _ => key
    };

    public static readonly string[] PreferredOrder =
    [
        EnsureDb,
        Backup,
        Restore
    ];
}