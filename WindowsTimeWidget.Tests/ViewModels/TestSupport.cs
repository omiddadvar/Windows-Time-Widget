using System.Windows.Threading;

namespace WindowsTimeWidget.Tests.ViewModels;

/// <summary>
/// Runs an action on a dedicated STA thread with a WPF Dispatcher,
/// required for types that construct DispatcherTimer / WPF objects.
/// </summary>
internal static class StaRunner
{
    public static void Run(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
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

