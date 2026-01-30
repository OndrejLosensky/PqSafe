using System.Diagnostics;
using PgSafe.Config;
using PgSafe.Models.Restore;

namespace PgSafe.Services;

public static class RestoreService
{
    public static RestoreRunResult Run(
        PgSafeConfig config,
        List<RestoreTarget> targets,
        Action<RestoreProgress>? onProgress = null
    )
    {
        var result = new RestoreRunResult();

        var total = targets.Count;
        var current = 0;

        foreach (var target in targets)
        {
            current++;

            onProgress?.Invoke(new RestoreProgress(
                target.InstanceName,
                target.DatabaseName,
                current,
                total
            ));

            try
            {
                var sw = Stopwatch.StartNew();

                RestoreDatabase(
                    target.InstanceName,
                    target.InstanceConfig,
                    target.DatabaseName,
                    target.DumpFile
                );

                sw.Stop();

                result.Successes.Add(
                    new RestoreSuccess
                    {
                        Instance = target.InstanceName,
                        Database = target.DatabaseName,
                        FilePath = target.DumpFile,
                        Duration = sw.Elapsed
                    }
                );
            }
            catch (Exception ex)
            {
                result.Failures.Add(
                    new RestoreFailure
                    {
                        Instance = target.InstanceName,
                        Database = target.DatabaseName,
                        Error = ex.Message
                    }
                );
            }
        }

        return result;
    }

    public static void RunSingle(
        string instanceName,
        PgInstanceConfig instance,
        string databaseName,
        string dumpFile
    )
    {
        RestoreDatabase(
            instanceName,
            instance,
            databaseName,
            dumpFile
        );
    }

    private static void RestoreDatabase(
        string instanceName,
        PgInstanceConfig instance,
        string databaseName,
        string dumpFile
    )
    {
        if (!File.Exists(dumpFile))
            throw new FileNotFoundException(
                $"Dump file not found: {dumpFile}"
            );

        var psi = new ProcessStartInfo
        {
            FileName = "pg_restore",
            Arguments =
                $"--clean --if-exists " +
                $"-h {instance.Host} " +
                $"-p {instance.Port} " +
                $"-U {instance.Username} " +
                $"-d {databaseName} " +
                $"\"{dumpFile}\"",
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
    }
}
