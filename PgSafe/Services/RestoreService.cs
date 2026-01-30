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
                target.Instance,
                target.Database,
                current,
                total
            ));

            try
            {
                var sw = Stopwatch.StartNew();

                RestoreDatabase(
                    target.Instance,
                    target.InstanceConfig,
                    target.Database,
                    target.DumpFilePath
                );

                sw.Stop();

                result.Successes.Add(
                    new RestoreSuccess
                    {
                        Instance = target.Instance,
                        Database = target.Database,
                        FilePath = target.DumpFilePath,
                        Duration = sw.Elapsed
                    }
                );
            }
            catch (Exception ex)
            {
                result.Failures.Add(
                    new RestoreFailure
                    {
                        Instance = target.Instance,
                        Database = target.Database,
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
        string dumpFilePath
    )
    {
        RestoreDatabase(
            instanceName,
            instance,
            databaseName,
            dumpFilePath
        );
    }

    private static void RestoreDatabase(
        string instanceName,
        PgInstanceConfig instance,
        string databaseName,
        string dumpFilePath
    )
    {
        if (!File.Exists(dumpFilePath))
            throw new FileNotFoundException(
                $"Dump file not found: {dumpFilePath}"
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
                $"\"{dumpFilePath}\"",
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
