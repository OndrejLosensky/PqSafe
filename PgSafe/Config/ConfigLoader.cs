using PgSafe.Utils;

namespace PgSafe.Config;

public static class ConfigLoader
{
    public static PgSafeConfig Load(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Config file not found: {fullPath}");

        var config = LoadYaml.LoadYamlFile(fullPath);

        foreach (var instance in config.Instances.Values)
        {
            instance.Username = EnvResolver.ResolveEnv(instance.Username);
            instance.Password = EnvResolver.ResolveEnv(instance.Password);
        }

        ConfigValidator.Validate(config);
        return config;
    }
}