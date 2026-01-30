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
        Action<TTarget, Exception, TimeSpan> failure
    )
    {
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
                foreach (var target in targets)
                {
                    var task = ctx.AddTask(label(target), maxValue: 100);
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        execute(target);

                        sw.Stop();
                        task.Value = 100;
                        success(target, sw.Elapsed);
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        task.StopTask();
                        failure(target, ex, sw.Elapsed);
                    }
                }
            });
    }
}
