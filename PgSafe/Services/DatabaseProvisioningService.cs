using Npgsql;
using PgSafe.Config;

namespace PgSafe.Services;

public static class DatabaseProvisioningService
{
    public static bool DatabaseExists(
        PgInstanceConfig instance,
        string databaseName
    )
    {
        using var conn = new NpgsqlConnection(
            BuildAdminConnectionString(instance)
        );
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT 1 FROM pg_database WHERE datname = @name;";
        cmd.Parameters.AddWithValue("name", databaseName);

        return cmd.ExecuteScalar() != null;
    }

    public static void CreateDatabase(
        PgInstanceConfig instance,
        string databaseName
    )
    {
        using var conn = new NpgsqlConnection(
            BuildAdminConnectionString(instance)
        );
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"CREATE DATABASE \"{databaseName}\";";

        cmd.ExecuteNonQuery();
    }

    private static string BuildAdminConnectionString(
        PgInstanceConfig instance
    )
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = instance.Host,
            Port = instance.Port,
            Username = instance.Username,
            Password = instance.Password,
            Database = "postgres",
            Timeout = 5,
            CommandTimeout = 5
        };

        if (!string.IsNullOrWhiteSpace(instance.RootCertificate))
            builder.RootCertificate = instance.RootCertificate;

        return builder.ConnectionString;
    }
}