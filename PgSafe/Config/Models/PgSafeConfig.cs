namespace PgSafe.Config;

public class PgSafeConfig
{
    public string OutputDir { get; set; } = "./backups";

    public List<DatabaseConfig> Databases { get; set; } = [];
}

public class DatabaseConfig
{
    public string Name { get; set; } = string.Empty;      // logical name
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;  // supports ${ENV}
    public string Password { get; set; } = string.Empty;  // supports ${ENV}
}