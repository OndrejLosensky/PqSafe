using Spectre.Console;
using System.Diagnostics;

namespace PgSafe.Cli.Runners;

public static class ProgressRunner
{
    public static void Run<TTarget>(
        IEnumerable<TTarget> targets,
        Func<TTarget, string> label,
        Action<TTarget> execute,
        Action<TTarget, TimeSpan> success,
        Action<TTarget, Exception, TimeSpan> failure,
        int parallelism = 1
    )
    {
        var semaphore = new SemaphoreSlim(parallelism);
        var resultLock = new object();

        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .Start(ctx =>
            {
                var tasks = targets.Select(target =>
                {
                    var progressTask = ctx.AddTask(
                        label(target),
                        maxValue: 100
                    );

                    return Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        var sw = Stopwatch.StartNew();

                        try
                        {
                            execute(target);

                            sw.Stop();
                            progressTask.Value = 100;

                            lock (resultLock)
                                success(target, sw.Elapsed);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            progressTask.StopTask();

                            lock (resultLock)
                                failure(target, ex, sw.Elapsed);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                }).ToArray();

                Task.WaitAll(tasks);
            });
    }
}
