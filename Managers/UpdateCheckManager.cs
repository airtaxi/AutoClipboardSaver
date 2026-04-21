using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Services.Store;
using Windows.System;

namespace AutoClipboardSaver;

public static class UpdateCheckManager
{
    private static readonly Uri s_storeDeepLink = new("ms-windows-store://pdp/?productid=9P9M0JWCJQTX");
    private static readonly Uri s_creatorGitHubRepositoryLink = new("https://github.com/airtaxi/AutoClipboardSaver");

    public static async Task<bool> HasStoreUpdateAsync()
    {
        var storeContext = StoreContext.GetDefault();
        var storePackageUpdates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
        return storePackageUpdates.Count > 0;
    }

    public static string GetCurrentPackageVersion()
    {
        var packageVersion = Package.Current.Id.Version;
        return $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
    }

    public static async Task<bool> OpenStorePageAsync() => await Launcher.LaunchUriAsync(s_storeDeepLink);
    public static async Task<bool> OpenCreatorGitHubRepositoryAsync() => await Launcher.LaunchUriAsync(s_creatorGitHubRepositoryLink);
}
