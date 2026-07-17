using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TiaGitAddIn.Tests.UI
{
    /// <summary>
    /// Runs WPF-affinitized code (template loading, Measure/Arrange/UpdateLayout, DataContext binding)
    /// on a dedicated STA thread with its own <see cref="Dispatcher"/>, since xunit test threads are MTA
    /// and none of the framework/control machinery this exercises tolerates that.
    /// </summary>
    internal static class WpfTestHost
    {
        public static void Run(Action<Dispatcher> action) =>
            RunAsync(dispatcher => { action(dispatcher); return Task.CompletedTask; }).GetAwaiter().GetResult();

        public static Task RunAsync(Func<Dispatcher, Task> action)
        {
            var completion = new TaskCompletionSource<object?>();
            var thread = new Thread(() =>
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                dispatcher.BeginInvoke(new Action(async () =>
                {
                    try { await action(dispatcher); completion.TrySetResult(null); }
                    catch (Exception ex) { completion.TrySetException(ex); }
                    finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
                }));
                Dispatcher.Run();
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
        }
    }
}
