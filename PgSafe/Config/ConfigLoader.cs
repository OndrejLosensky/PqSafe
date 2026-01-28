namespace PgSafe.Config;

public static class ConfigLoader
{
    public static PgSafeConfig Load(string path)
    {
        return new PgSafeConfig();
    }
}