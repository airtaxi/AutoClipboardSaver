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

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        s_settingsWindow = new SettingsWindow();

        s_clipboardMonitor = new ClipboardMonitor();
        s_clipboardMonitor.Start();
    }

    public static void ShowSettingsWindow() => s_settingsWindow.Activate();
}
