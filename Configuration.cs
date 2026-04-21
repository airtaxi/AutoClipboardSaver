using Microsoft.Windows.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;

namespace AutoClipboardSaver;

public static class Configuration
{
    private static readonly ApplicationDataContainer s_localSettings = ApplicationData.GetDefault().LocalSettings;
    private static readonly string[] s_supportedLanguageTags = ["en", "ko", "ja", "zh-Hans", "zh-Hant"];

    public static string LanguageTag
    {
        get
        {
            s_localSettings.Values.TryGetValue("LanguageTag", out object value);
            if (value is string storedLanguageTag && !string.IsNullOrWhiteSpace(storedLanguageTag))
                return GetSupportedLanguageTag(storedLanguageTag);

            foreach (var preferredLanguageTag in ApplicationLanguages.Languages)
            {
                if (TryGetSupportedLanguageTag(preferredLanguageTag, out string supportedLanguageTag))
                    return supportedLanguageTag;
            }

            return "en";
        }
        set => s_localSettings.Values["LanguageTag"] = GetSupportedLanguageTag(value);
    }

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

    public static int ExpirationMinutes
    {
        get
        {
            s_localSettings.Values.TryGetValue("ExpirationMinutes", out object value);
            if (value == null) return -1;
            return (int)value;
        }
        set => s_localSettings.Values["ExpirationMinutes"] = value;
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

    public static string SaveFileFormat
    {
        get
        {
            s_localSettings.Values.TryGetValue("SaveFileFormat", out object value);
            if (value == null) return "jpg";
            return (string)value;
        }
        set => s_localSettings.Values["SaveFileFormat"] = value;
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

    private static string GetSupportedLanguageTag(string languageTag)
    {
        if (TryGetSupportedLanguageTag(languageTag, out string supportedLanguageTag))
            return supportedLanguageTag;

        return "en";
    }

    private static bool TryGetSupportedLanguageTag(string languageTag, out string supportedLanguageTag)
    {
        supportedLanguageTag = string.Empty;
        if (string.IsNullOrWhiteSpace(languageTag)) return false;

        if (languageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            if (languageTag.Contains("Hant", StringComparison.OrdinalIgnoreCase) ||
                languageTag.Contains("TW", StringComparison.OrdinalIgnoreCase) ||
                languageTag.Contains("HK", StringComparison.OrdinalIgnoreCase) ||
                languageTag.Contains("MO", StringComparison.OrdinalIgnoreCase))
            {
                supportedLanguageTag = "zh-Hant";
                return true;
            }

            supportedLanguageTag = "zh-Hans";
            return true;
        }

        foreach (var supportedLanguageCandidateTag in s_supportedLanguageTags)
        {
            if (supportedLanguageCandidateTag.Equals(languageTag, StringComparison.OrdinalIgnoreCase))
            {
                supportedLanguageTag = supportedLanguageCandidateTag;
                return true;
            }
            if (languageTag.StartsWith($"{supportedLanguageCandidateTag}-", StringComparison.OrdinalIgnoreCase))
            {
                supportedLanguageTag = supportedLanguageCandidateTag;
                return true;
            }
        }

        return false;
    }
}
