using System.Windows;
using System.Windows.Threading;

namespace WindowsTimeWidget.Tests.ViewModels;

/// <summary>
/// Runs an action on a dedicated STA thread with a WPF Dispatcher and a
/// bare-bones Application (so Application.Current is non-null and
/// resource lookups don't throw).
/// </summary>
internal static class StaRunner
{
    public static void Run(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            // Create an Application only if none exists yet on this thread.
            var app = Application.Current;
            if (app is null)
            {
                app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (captured is not null)
            throw captured;
    }

    public static T Run<T>(Func<T> func)
    {
        T result = default!;
        Run(() => { result = func(); });
        return result;
    }
}