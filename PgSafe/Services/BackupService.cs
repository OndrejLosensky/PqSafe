using System.Diagnostics;
using PgSafe.Config;
using PgSafe.Models.Backup;

namespace PgSafe.Services;

public static class BackupService
{
    public static BackupRunResult Run(
        PgSafeConfig config,
        Action<BackupProgress>? onProgress = null
    )
    {
        var result = new BackupRunResult();

        var allTargets = config.Instances
            .SelectMany(i =>
                i.Value.Databases
                    .Where(d => d.Value.Backup.Enabled)
                    .Select(d => (instance: i.Key, db: d.Key, cfg: i.Value))
            )
            .ToList();

        var total = allTargets.Count;
        var current = 0;

        foreach (var target in allTargets)
        {
            current++;

            onProgress?.Invoke(new BackupProgress(
                target.instance,
                target.db,
                current,
                total
            ));

            try
            {
                var sw = Stopwatch.StartNew();

                var file = BackupDatabase(
                    config.OutputDir,
                    target.instance,
                    target.cfg,
                    target.db
                );

                sw.Stop();

                result.Successes.Add(
                    new BackupSuccess
                    {
                        Instance = target.instance,
                        Database = target.db,
                        FilePath = file,
                        Duration = sw.Elapsed
                    }
                );
            }
            catch (Exception ex)
            {
                result.Failures.Add(
                    new BackupFailure
                    {
                        Instance = target.instance,
                        Database = target.db,
                        Error = ex.Message
                    }
                );
            }

        }

        return result;
    }
    
    public static string  RunSingle(
        string outputDir,
        string instanceName,
        PgInstanceConfig instance,
        string databaseName
    )
    {
        return BackupDatabase(outputDir, instanceName, instance, databaseName);
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

        var finalFile = Path.Combine(targetDir, $"{timestamp}.dump");
        var tempFile = finalFile + ".tmp";

        var psi = new ProcessStartInfo
        {
            FileName = "pg_dump",
            Arguments =
                $"-h {instance.Host} " +
                $"-p {instance.Port} " +
                $"-U {instance.Username} " +
                $"-F c " +
                $"-f \"{tempFile}\" " +
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

            if (File.Exists(tempFile))
                File.Delete(tempFile);

            throw new Exception(error);
        }

        File.Move(tempFile, finalFile);
        
        return finalFile;
    }
}
