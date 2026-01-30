using PgSafe.Utils;
using PgSafe.Services;

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

        // AUTO-DETECT DATABASES
        foreach (var (instanceName, instance) in config.Instances)
        {
            if (!instance.AutoDetect)
                continue;

            Console.Error.WriteLine(
                $"[PgSafe - Debug] Auto-detect: instance='{instanceName}', host='{instance.Host}', port={instance.Port}, user='{instance.Username}'"
            );

            try
            {
                var databases = DatabaseDiscoveryService.DiscoverDatabases(instance);

                Console.Error.WriteLine(
                    $"[PgSafe - Debug] Auto-detect: instance='{instanceName}' discovered {databases.Count} databases."
                );

                instance.Databases.Clear();

                foreach (var db in databases)
                {
                    instance.Databases[db] = new PgDatabaseConfig();
                }
            }
            catch (Exception ex)
            {
                // Do not abort the entire config load if one instance can't connect.
                Console.Error.WriteLine(
                    $"[PgSafe - Debug] WARNING: Auto-detect failed for instance '{instanceName}' ({instance.Host}:{instance.Port}, user='{instance.Username}'): {ex.Message}"
                );

                // Leave instance.Databases as-is (likely empty) and continue.
                // The UI will naturally have nothing to select for this instance.
            }
        }

        ConfigValidator.Validate(config);

        return config;
    }
}