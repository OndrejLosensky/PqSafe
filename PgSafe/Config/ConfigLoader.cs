using PgSafe.Config;
using PgSafe.Utils;

namespace PgSafe.Config;

public static class ConfigLoader
{
    public static PgSafeConfig Load(string path)
    {
        var config = LoadYaml.LoadYamlFile(path);

        foreach (var db in config.Databases)
        {
            db.Username = EnvResolver.ResolveEnv(db.Username);
            db.Password = EnvResolver.ResolveEnv(db.Password);
        }

        ConfigValidator.Validate(config);
        return config;
    }
}