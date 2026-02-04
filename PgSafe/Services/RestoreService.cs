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
        string dumpFile,
        Action<double>? progressCallback = null
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
            throw new FileNotFoundException($"Dump file not found: {dumpFile}");

        var args =
            $"--clean --if-exists " +
            $"-h {instance.Host} " +
            $"-p {instance.Port} " +
            $"-U {instance.Username} " +
            $"-d {databaseName} " +
            $"\"{dumpFile}\"";

        var psi = new ProcessStartInfo
        {
            FileName = "pg_restore",
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        // Sensitive: do not log/print this.
        psi.Environment["PGPASSWORD"] = instance.Password;

        using var process = Process.Start(psi);
        if (process is null)
            throw new Exception("Failed to start pg_restore process.");

        // Start reading immediately to avoid deadlocks
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        var stdout = stdOutTask.GetAwaiter().GetResult();
        var stderr = stdErrTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            var stderrFirstLine = FirstNonEmptyLine(stderr);

            var msg =
                $"pg_restore failed (exit {process.ExitCode}) for {instanceName}/{databaseName}"
                + (string.IsNullOrWhiteSpace(stderrFirstLine) ? "." : $": {stderrFirstLine}");

            var ex = new Exception(msg);
            ex.Data["pg_restore.exitCode"] = process.ExitCode;
            ex.Data["pg_restore.args"] = args; // safe: no password included
            ex.Data["pg_restore.stdout"] = stdout;
            ex.Data["pg_restore.stderr"] = stderr;

            throw ex;
        }
    }

    private static string? FirstNonEmptyLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        return null;
    }


    private static string Trim(string s, int maxLen)
    {
        s = s.Trim();
        return s.Length <= maxLen ? s : s.Substring(0, maxLen - 1) + "…";
    }
}