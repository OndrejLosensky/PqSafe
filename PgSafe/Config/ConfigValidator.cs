namespace PgSafe.Config;

public static class ConfigValidator
{
    public static void Validate(PgSafeConfig config)
    {
        if (config.Databases == null || config.Databases.Count == 0)
            throw new Exception("No databases configured");

        foreach (var db in config.Databases)
        {
            if (string.IsNullOrWhiteSpace(db.Name))
                throw new Exception("Database name is required");

            if (string.IsNullOrWhiteSpace(db.Host))
                throw new Exception($"Database '{db.Name}' is missing host");

            if (db.Port <= 0)
                throw new Exception($"Database '{db.Name}' has invalid port");

            if (string.IsNullOrWhiteSpace(db.Username))
                throw new Exception($"Database '{db.Name}' username is required");

            if (string.IsNullOrWhiteSpace(db.Password))
                throw new Exception($"Database '{db.Name}' password is required");

            if (string.IsNullOrWhiteSpace(db.Database))
                throw new Exception($"Database '{db.Name}' database name is required");
        }
    }
}