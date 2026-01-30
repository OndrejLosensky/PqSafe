namespace PgSafe.Config;

public static class ConfigValidator
{
    public static void Validate(PgSafeConfig config)
    {
        if (config.Instances == null || config.Instances.Count == 0)
            throw new Exception("No instances configured");

        foreach (var (instanceName, instance) in config.Instances)
        {
            if (string.IsNullOrWhiteSpace(instance.Host))
                throw new Exception($"Instance '{instanceName}' is missing host");

            if (instance.Port <= 0)
                throw new Exception($"Instance '{instanceName}' has invalid port");

            if (string.IsNullOrWhiteSpace(instance.Username))
                throw new Exception($"Instance '{instanceName}' username is required");

            if (string.IsNullOrWhiteSpace(instance.Password))
                throw new Exception($"Instance '{instanceName}' password is required");

            if (instance.Databases.Count == 0 && !instance.AutoDetect)
            {
                throw new Exception(
                    $"Instance '{instanceName}' has no databases configured and autoDetect is disabled"
                );
            }

            foreach (var (dbName, _) in instance.Databases)
            {
                if (string.IsNullOrWhiteSpace(dbName))
                    throw new Exception($"Instance '{instanceName}' has database with empty name");
            }
        }
    }
}