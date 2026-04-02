using Microsoft.UI.Xaml;

namespace AutoClipboardSaver;

public partial class App : Application
{
    private static Window s_settingsWindow;
    private static ClipboardMonitor s_clipboardMonitor;

    public static Window SettingsWindow => s_settingsWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        s_clipboardMonitor = new ClipboardMonitor();
        s_clipboardMonitor.Start();

        s_settingsWindow = new SettingsWindow();
    }

    public static void SetClipboardRecording(bool isRecording) => s_clipboardMonitor.IsRecording = isRecording;

    public static void ShowSettingsWindow() => s_settingsWindow.Activate();
}
