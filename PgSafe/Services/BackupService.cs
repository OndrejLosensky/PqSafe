using System.Diagnostics;
using System.Text.Json;
using PgSafe.Config;
using PgSafe.Models.Backup;
using Npgsql;

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

                var file = RunSingle(
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
                        FilePath = file.DumpPath,
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

    public static BackupSet RunSingle(
        string outputDir,
        string instanceName,
        PgInstanceConfig instance,
        string databaseName,
        Action<double>? progressCallback = null
    )
    {
        return BackupDatabase(
            outputDir,
            instanceName,
            instance,
            databaseName,
            progressCallback
        );
    }

    private static BackupSet BackupDatabase(
        string outputDir,
        string instanceName,
        PgInstanceConfig instance,
        string databaseName,
        Action<double>? progressCallback
    )
    {
        // 0% — starting
        progressCallback?.Invoke(0);

        var backupId = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");

        var backupDir = Path.Combine(
            outputDir,
            instanceName,
            databaseName,
            backupId
        );

        Directory.CreateDirectory(backupDir);

        var finalFile = Path.Combine(backupDir, $"{databaseName}.dump");
        var tempFile = finalFile + ".tmp";

        // 10% — preparation done
        progressCallback?.Invoke(10);

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

        // 90% — dump completed
        progressCallback?.Invoke(90);

        // Get pg_dump version
        string pgVersion;
        try
        {
            var psiVersion = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var procVersion = Process.Start(psiVersion)!;
            procVersion.WaitForExit();
            pgVersion = procVersion.StandardOutput.ReadToEnd().Trim();
        }
        catch
        {
            pgVersion = "unknown";
        }

        var (tables, rows) = GetDatabaseStats(instance, databaseName);

        var meta = new BackupMetadata
        {
            Instance = instanceName,
            Database = databaseName,
            BackupId = backupId,
            CreatedAt = DateTime.UtcNow,
            SizeBytes = new FileInfo(finalFile).Length,
            Format = "custom",
            PgVersion = pgVersion,
            TableCount = tables,
            RowCount = rows
        };

        var metaPath = Path.Combine(backupDir, "meta.json");

        File.WriteAllText(
            metaPath,
            JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );

        // 100% — fully done
        progressCallback?.Invoke(100);

        return new BackupSet
        {
            Instance = instanceName,
            Database = databaseName,
            BackupId = backupId,

            BackupDirectory = backupDir,
            DumpPath = finalFile,
            MetaPath = metaPath,

            Metadata = meta
        };
    }

    private static (int tableCount, long rowCount) GetDatabaseStats(
        PgInstanceConfig instance,
        string database
    )
    {
        using var conn = new NpgsqlConnection(
            $"Host={instance.Host};Port={instance.Port};Username={instance.Username};Password={instance.Password};Database={database}"
        );

        conn.Open();

        var tableCount = Convert.ToInt32(
            new NpgsqlCommand(
                "SELECT count(*) FROM information_schema.tables WHERE table_schema='public';",
                conn
            ).ExecuteScalar()
        );

        var rowCount = Convert.ToInt64(
            new NpgsqlCommand(
                "SELECT sum(n_live_tup) FROM pg_stat_user_tables;",
                conn
            ).ExecuteScalar()
        );

        return (tableCount, rowCount);
    }
}
