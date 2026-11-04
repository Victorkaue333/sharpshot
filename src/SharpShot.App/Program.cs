using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using SharpShot.App.Interop;
using SharpShot.App.Services;

namespace SharpShot.App;

/// <summary>
/// Custom entry point: handles toast activation without a UI, enforces a single instance
/// (a second launch just brings the first window to front), then starts the XAML application.
/// </summary>
public static class Program
{
    public const string InstanceKey = "SharpShot.MainInstance";

    [STAThread]
    public static int Main(string[] args)
    {
        XamlCheckProcessRequirements();
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activation.Kind == ExtendedActivationKind.AppNotification && activation.Data is AppNotificationActivatedEventArgs toast)
        {
            // Launched by a toast button after the app had exited: run the action and leave.
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            NotificationService.HandleActivation(toast.Arguments, new ShellService(loggerFactory.CreateLogger<ShellService>()));
            return 0;
        }

        var mainInstance = AppInstance.FindOrRegisterForKey(InstanceKey);
        if (!mainInstance.IsCurrent)
        {
            RedirectActivation(mainInstance, activation);
            return 0;
        }

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App(mainInstance, args);
        });
        return 0;
    }

    /// <summary>
    /// Redirects this activation to the running instance. The STA thread must keep pumping COM
    /// while the async redirect completes, hence the CoWaitForMultipleObjects pattern from the docs.
    /// </summary>
    private static void RedirectActivation(AppInstance target, AppActivationArguments activation)
    {
        var handle = User32.CreateEventW(0, true, false, null);
        if (handle == 0)
        {
            target.RedirectActivationToAsync(activation).AsTask().GetAwaiter().GetResult();
            return;
        }

        try
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await target.RedirectActivationToAsync(activation);
                }
                finally
                {
                    User32.SetEvent(handle);
                }
            });

            User32.CoWaitForMultipleObjects(User32.CwmoDefault, User32.Infinite, 1, [handle], out _);
        }
        finally
        {
            User32.CloseHandle(handle);
        }
    }

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();
}
