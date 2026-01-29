namespace PgSafe.Config;

public class PgSafeConfig
{
    public string OutputDir { get; set; } = "./backups";

    // instance name -> config
    public Dictionary<string, PgInstanceConfig> Instances { get; set; } = new();
}