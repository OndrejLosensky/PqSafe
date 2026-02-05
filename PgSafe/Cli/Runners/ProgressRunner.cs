using Spectre.Console;
using System.Diagnostics;

namespace PgSafe.Cli.Runners;

public static class ProgressRunner
{
    // Existing API (kept for compatibility)
    public static TimeSpan Run<TTarget>(
        IEnumerable<TTarget> targets,
        Func<TTarget, string> label,
        Action<TTarget> execute,
        Action<TTarget, TimeSpan> success,
        Action<TTarget, Exception, TimeSpan> failure,
        int parallelism = 1
    )
    {
        return Run(
            targets,
            label,
            (t, report) =>
            {
                execute(t);
                report(100);
            },
            success,
            failure,
            parallelism
        );
    }

    // New API: execute can report progress [0..100]
    public static TimeSpan Run<TTarget>(
        IEnumerable<TTarget> targets,
        Func<TTarget, string> label,
        Action<TTarget, Action<double>> execute,
        Action<TTarget, TimeSpan> success,
        Action<TTarget, Exception, TimeSpan> failure,
        int parallelism = 1
    )
    {
        var semaphore = new SemaphoreSlim(parallelism);
        var resultLock = new object();
        var swTotal = Stopwatch.StartNew(); // <<< start total stopwatch

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
                    var progressTask = ctx.AddTask(label(target), maxValue: 100);

                    void Report(double value)
                    {
                        // Clamp + monotonic (avoid accidental backwards progress)
                        var clamped = Math.Clamp(value, 0, 100);
                        if (clamped > progressTask.Value)
                            progressTask.Value = clamped;
                    }

                    return Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        var sw = Stopwatch.StartNew();

                        try
                        {
                            execute(target, Report);

                            sw.Stop();
                            Report(100);

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

        swTotal.Stop(); // <<< stop total stopwatch
        return swTotal.Elapsed; // <<< return the total wall-clock time
    }
}