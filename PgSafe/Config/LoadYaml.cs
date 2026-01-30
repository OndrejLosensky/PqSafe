using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PgSafe.Config;

public static class LoadYaml
{
    public static PgSafeConfig LoadYamlFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}");

        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            // YAML uses outputDir / dryRun / autoDetect (camelCase),
            // so this convention must match or AutoDetect will stay false.
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        PgSafeConfig? config;
        try
        {
            config = deserializer.Deserialize<PgSafeConfig>(yaml);
        }
        catch (Exception ex)
        {
            throw new Exception($"Exception during deserialization: {ex.Message}", ex);
        }

        if (config is null)
            throw new Exception("Failed to parse pgsafe.yml");
        
        return config;
    }
}