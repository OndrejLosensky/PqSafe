using Npgsql;
using PgSafe.Config;

namespace PgSafe.Services;

public static class DatabaseDiscoveryService
{
    public static IReadOnlyList<string> DiscoverDatabases(
        PgInstanceConfig instance
    )
    {
        var databases = new List<string>();

        var connectionString = BuildConnectionString(instance);

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
                          SELECT datname
                          FROM pg_database
                          WHERE datistemplate = false
                          ORDER BY datname;
                          ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            databases.Add(reader.GetString(0));
        }

        return databases;
    }

    private static string BuildConnectionString(
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

        /*
        if (!string.IsNullOrWhiteSpace(instance.SslMode))
        {
            builder.SslMode = instance.SslMode;
        }
        */

        if (!string.IsNullOrWhiteSpace(instance.RootCertificate))
            builder.RootCertificate = instance.RootCertificate;

        return builder.ConnectionString;
    }
}