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
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var config = deserializer.Deserialize<PgSafeConfig>(yaml);

        if (config is null)
            throw new Exception("Failed to parse pgsafe.yml");

        return config;
    }
}