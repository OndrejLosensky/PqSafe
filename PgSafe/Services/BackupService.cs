using System.Diagnostics;
using PgSafe.Config;
using PgSafe.Models;

namespace PgSafe.Services;

public static class BackupService
{
    public static BackupRunResult Run(PgSafeConfig config)
    {
        var result = new BackupRunResult();

        foreach (var (instanceName, instance) in config.Instances)
        {
            foreach (var (dbName, db) in instance.Databases)
            {
                if (!db.Backup.Enabled)
                    continue;

                try
                {
                    var file = BackupDatabase(
                        config.OutputDir,
                        instanceName,
                        instance,
                        dbName
                    );

                    result.Successes.Add(
                        new BackupSuccess(instanceName, dbName, file)
                    );
                }
                catch (Exception ex)
                {
                    result.Failures.Add(
                        new BackupFailure(instanceName, dbName, ex.Message)
                    );

                    Console.WriteLine(
                        $"Backup failed: {instanceName}/{dbName}\n{ex.Message}"
                    );
                }
            }
        }

        return result;
    }


    private static string BackupDatabase(
        string outputDir,
        string instanceName,
        PgInstanceConfig instance,
        string databaseName
    )
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");

        var targetDir = Path.Combine(outputDir, instanceName, databaseName);
        Directory.CreateDirectory(targetDir);

        var outputFile = Path.Combine(targetDir, $"{timestamp}.dump");

        Console.WriteLine($"Backing up {instanceName}/{databaseName}");

        var psi = new ProcessStartInfo
        {
            FileName = "pg_dump",
            Arguments =
                $"-h {instance.Host} " +
                $"-p {instance.Port} " +
                $"-U {instance.Username} " +
                $"-F c " +
                $"-f \"{outputFile}\" " +
                databaseName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        psi.Environment["PGPASSWORD"] = instance.Password;

        using var process = Process.Start(psi)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new Exception(error);
        }

        Console.WriteLine($"Backup completed: {instanceName}/{databaseName}");
        return outputFile;
    }
}
