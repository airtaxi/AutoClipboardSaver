using Microsoft.Windows.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace AutoClipboardSaver;

public static class Configuration
{
    private static readonly ApplicationDataContainer s_localSettings = ApplicationData.GetDefault().LocalSettings;

    public static int MaxImages
    {
        get
        {
            s_localSettings.Values.TryGetValue("MaxImages", out object value);
            if (value == null) return 15;
            return (int)value;
        }

        set => s_localSettings.Values["MaxImages"] = value;
    }

    public static string SaveDirectoryPath
    {
        get
        {
            s_localSettings.Values.TryGetValue("SaveDirectoryPath", out object value);

            if (value == null) return GetDefaultSaveDirectoryPath();

            var path = (string)value;
            if (!Path.Exists(path)) return GetDefaultSaveDirectoryPath();
            else return path;
        }
        set => s_localSettings.Values["SaveDirectoryPath"] = value;
    }

    public static bool SaveWithTimestamp
    {
        get
        {
            s_localSettings.Values.TryGetValue("SaveWithTimestamp", out object value);
            if (value == null) return true;
            return (bool)value;
        }
        set => s_localSettings.Values["SaveWithTimestamp"] = value;
    }

    public static bool LaunchOnStartup
    {
        get
        {
            var startupTask = Task.Run(async () => await StartupTask.GetAsync("AutoClipboardSaverStartup")).GetAwaiter().GetResult();
            return startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }
        set
        {
            var startupTask = Task.Run(async () => await StartupTask.GetAsync("AutoClipboardSaverStartup")).GetAwaiter().GetResult();
            if (value) Task.Run(async () => await startupTask.RequestEnableAsync()).GetAwaiter().GetResult();
            else startupTask.Disable();
        }
    }

    private static string GetDefaultSaveDirectoryPath()
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var saveDirectoryPath = Path.Combine(picturesPath, "AutoClipboardSaver");
        if (!Directory.Exists(saveDirectoryPath)) Directory.CreateDirectory(saveDirectoryPath);
        return saveDirectoryPath;
    }
}
