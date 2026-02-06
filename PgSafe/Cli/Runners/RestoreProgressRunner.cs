using PgSafe.Models.Restore;
using PgSafe.Models.Backup;
using PgSafe.Models;
using PgSafe.Services;
using PgSafe.Config;
using PgSafe.Utils;

namespace PgSafe.Cli.Runners;

public static class RestoreProgressRunner
{
    public static RestoreRunResult Run(
        BackupSet backupSet,
        PgInstanceConfig instanceConfig,
        string targetDatabase
    )
    {
        var result = new RestoreRunResult();

        var target = new RestoreTarget(
            backupSet.Instance,
            instanceConfig,
            targetDatabase,
            backupSet.DumpPath
        );

        ProgressRunner.Run(
            new[] { target },
            t => $"{t.InstanceName}/{t.DatabaseName}",

            (t, report, setStep) =>
            {
                setStep(StepKeys.ToLabel(StepKeys.Restore));
                report(0);

                RestoreService.RunSingle(
                    t.InstanceName,
                    t.InstanceConfig,
                    t.DatabaseName,
                    t.DumpFile
                );

                report(100);
            },

            (t, d) => result.Successes.Add(new RestoreSuccess
            {
                Instance = t.InstanceName,
                Database = t.DatabaseName,
                FilePath = t.DumpFile,
                FileSizeBytes = FileUtils.GetFileSize(t.DumpFile),
                Duration = d,
                StepDurations = new Dictionary<string, TimeSpan>
                {
                    [StepKeys.Restore] = d
                }
            }),

            (t, ex, d) =>
            {
                var details = ex.ToString();
                var logPath = TryWriteRestoreFailureLog(t.InstanceName, t.DatabaseName, details);

                var preferred = ExtractPgRestoreStderrFirstLine(ex);
                var error = string.IsNullOrWhiteSpace(preferred) ? ex.Message : preferred!;

                if (!string.IsNullOrWhiteSpace(logPath))
                    error += $" (details: {logPath})";

                result.Failures.Add(new RestoreFailure
                {
                    Instance = t.InstanceName,
                    Database = t.DatabaseName,
                    Error = error,
                    Details = details,
                    LogFilePath = logPath,
                    Duration = d,
                    StepDurations = new Dictionary<string, TimeSpan>
                    {
                        [StepKeys.Restore] = d
                    }
                });
            }
        );

        return result;
    }

    private static string? ExtractPgRestoreStderrFirstLine(Exception ex)
    {
        foreach (var e in EnumerateExceptionChain(ex))
        {
            if (e.Data is null || e.Data.Count == 0)
                continue;

            if (e.Data.Contains("pg_restore.stderr") && e.Data["pg_restore.stderr"] is string stderr)
                return FirstNonEmptyLine(stderr);
        }

        return null;
    }

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException!)
            yield return cur;
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

    private static string? TryWriteRestoreFailureLog(string instance, string database, string details)
    {
        try
        {
            var safeInstance = MakeFileNameSafe(instance);
            var safeDb = MakeFileNameSafe(database);

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "pgsafe",
                "logs",
                "restore"
            );

            Directory.CreateDirectory(dir);

            var file = $"restore-failed-{safeInstance}-{safeDb}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log";
            var path = Path.Combine(dir, file);

            File.WriteAllText(path, details);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static string MakeFileNameSafe(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value;
    }
}