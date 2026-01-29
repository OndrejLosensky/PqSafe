namespace PgSafe.Config;

public class PgInstanceConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;

    // supports ${ENV}
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // database name -> config
    public Dictionary<string, PgDatabaseConfig> Databases { get; set; } = new();
}