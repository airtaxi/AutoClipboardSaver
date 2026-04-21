using Microsoft.UI.Xaml;
using Windows.Globalization;

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
        ApplyLanguage(Configuration.LanguageTag);

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

    public static void SetLanguage(string languageTag)
    {
        Configuration.LanguageTag = languageTag;
        ApplyLanguage(Configuration.LanguageTag);
        if (s_settingsWindow != null) s_settingsWindow.DispatcherQueue.TryEnqueue(() => s_settingsWindow.RefreshLocalizedContent());
    }

    private static void ApplyLanguage(string languageTag) => ApplicationLanguages.PrimaryLanguageOverride = languageTag;
}
