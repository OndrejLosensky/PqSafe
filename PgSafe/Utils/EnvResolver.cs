namespace PgSafe.Utils;

public static class EnvResolver
{
    public static string ResolveEnv(string value)
    {
        if (!value.StartsWith("${") || !value.EndsWith("}"))
            return value;

        var envName = value[2..^1];
        var envValue = Environment.GetEnvironmentVariable(envName);

        if (string.IsNullOrWhiteSpace(envValue))
            throw new InvalidOperationException(
                $"Environment variable '{envName}' is not set.");

        return envValue;
    }
}