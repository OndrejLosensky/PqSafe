namespace PgSafe.Config;

public class PgInstanceConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;

    // supports ${ENV}
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // SSL / connection options
    public string? SslMode { get; set; }          // e.g. disable, require, verify-ca, verify-full
    public string? RootCertificate { get; set; }  // path to ca.pem

    // when true, databases are discovered automatically
    public bool AutoDetect { get; set; } = false;

    // database name -> config (ignored when AutoDetect = true)
    public Dictionary<string, PgDatabaseConfig> Databases { get; set; } = new();
}