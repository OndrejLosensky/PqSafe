using Npgsql;
using PgSafe.Config;

namespace PgSafe.Utils;

public static class DatabaseUtils
{
    public static bool DatabaseExists(PgInstanceConfig instance, string databaseName)
    {
        try
        {
            using var conn = new NpgsqlConnection(
                $"Host={instance.Host};Port={instance.Port};Username={instance.Username};Password={instance.Password};Database=postgres"
            );
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @name",
                conn
            );
            cmd.Parameters.AddWithValue("name", databaseName);

            var exists = cmd.ExecuteScalar();
            return exists != null;
        }
        catch
        {
            return false;
        }
    }
}