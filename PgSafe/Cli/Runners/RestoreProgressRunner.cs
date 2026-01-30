using PgSafe.Models.Restore;
using PgSafe.Services;
using PgSafe.Config;
using PgSafe.Utils;

namespace PgSafe.Cli.Runners;

public static class RestoreProgressRunner
{
    public static RestoreRunResult Run(
        string instanceName,
        PgInstanceConfig instance,
        string database,
        string dumpFile
    )
    {
        var result = new RestoreRunResult();

        var target = new RestoreTarget(
            instanceName,
            instance,
            database,
            dumpFile
        );

        ProgressRunner.Run(
            new[] { target },
            t => $"{t.InstanceName}/{t.DatabaseName}",

            t => RestoreService.RunSingle(
                t.InstanceName,
                t.InstanceConfig,
                t.DatabaseName,
                t.DumpFile
            ),

            (t, d) => result.Successes.Add(new RestoreSuccess
            {
                Instance = t.InstanceName,
                Database = t.DatabaseName,
                FilePath = t.DumpFile,
                FileSizeBytes = FileUtils.GetFileSize(t.DumpFile),
                Duration = d
            }),

            (t, ex, d) => result.Failures.Add(new RestoreFailure
            {
                Instance = t.InstanceName,
                Database = t.DatabaseName,
                Error = ex.Message,
                Duration = d
            })
        );

        return result;
    }
}
