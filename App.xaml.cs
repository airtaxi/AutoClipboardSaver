using Microsoft.UI.Xaml;

namespace AutoClipboardSaver;

public partial class App : Application
{
    private static SettingsWindow s_settingsWindow;
    private static ClipboardMonitor s_clipboardMonitor;

    public static SettingsWindow SettingsWindow => s_settingsWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs launchActivatedEventArgs)
    {
        s_clipboardMonitor = new ClipboardMonitor();
        s_clipboardMonitor.Start();

        s_settingsWindow = new SettingsWindow();
    }

    public static void SetClipboardRecording(bool isRecording) => s_clipboardMonitor.IsRecording = isRecording;

    public static void ShowSettingsWindow()
    {
        s_settingsWindow.DispatcherQueue.TryEnqueue(() =>
        {
            s_settingsWindow.Activate();
            s_settingsWindow.BringToFront();
        });
    }

    public static void Shutdown()
    {
        s_clipboardMonitor?.Dispose();
        s_settingsWindow?.ForceClose();
    }
}
