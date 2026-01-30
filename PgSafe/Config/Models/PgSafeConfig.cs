namespace PgSafe.Config;

public class PgSafeConfig
{
    public string OutputDir { get; set; } = "./backups";

    // instance name -> config
    public Dictionary<string, PgInstanceConfig> Instances { get; set; } = new();
    
    // Runtime flags
    public bool DryRun { get; set; }
    
    public int Parallelism { get; set; } = 1;
}