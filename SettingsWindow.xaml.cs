using AutoClipboardSaver.Pages;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUIEx;

namespace AutoClipboardSaver;

public sealed partial class SettingsWindow : WindowEx
{
    private const uint WindowMessageClose = 0x0010;
    private const uint WindowMessageQueryEndSession = 0x0011;
    private const uint WindowMessageEndSession = 0x0016;

    private delegate nint WindowSubclassProcedure(nint windowHandle, uint message, nint windowParameter, nint longParameter, nuint subclassIdentifier, nuint referenceData);

    [LibraryImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(nint windowHandle, WindowSubclassProcedure procedure, nuint subclassIdentifier, nuint referenceData);

    [LibraryImport("comctl32.dll")]
    private static partial nint DefSubclassProc(nint windowHandle, uint message, nint windowParameter, nint longParameter);

    private int _lastResizedHeight;
    private bool _isResizing;
    private readonly WindowSubclassProcedure _windowSubclassProcedure;
    private bool _forceClose;
    private bool _systemShutdown;

    private static SettingsWindow Instance { get; set; }

    public SettingsWindow()
    {
        InitializeComponent();
        Instance = this;

        // Set window icons
        AppWindow.SetIcon("Assets/logo.ico");

        // Title bar customization
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Since Activate() isn't called by default, we can set up the TaskbarIcon commands here
        TaskbarIcon.LeftClickCommand = OpenSettingsWindowCommand;
        OpenSettingsMenuFlyoutItem.Command = OpenSettingsWindowCommand;

        _windowSubclassProcedure = WindowSubclassProc;
        SetWindowSubclass(this.GetWindowHandle(), _windowSubclassProcedure, 1, 0);

        RefreshLocalizedContent();
    }

    public static void ShowLoading(string message)
    {
        Instance.DispatcherQueue.TryEnqueue(() =>
        {
            Instance.AppFrame.IsEnabled = false;
            Instance.LoadingGrid.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(message))
            {
                Instance.LoadingTextBlock.Text = message;
                Instance.LoadingTextBlock.Visibility = Visibility.Visible;
            }
            else Instance.LoadingTextBlock.Visibility = Visibility.Collapsed;
        });
    }

    public static void HideLoading()
    {
        Instance.DispatcherQueue.TryEnqueue(() =>
        {
            Instance.LoadingGrid.Visibility = Visibility.Collapsed;
            Instance.LoadingTextBlock.Visibility = Visibility.Collapsed;
            Instance.AppFrame.IsEnabled = true;
        });
    }

    private void ResizeWindowToContent(FrameworkElement element, bool updateMeasure)
    {
        if (_isResizing || element?.XamlRoot == null) return;
        _isResizing = true;

        try
        {
            var scale = element.XamlRoot.RasterizationScale;
            var width = (int)(300 * scale);

            if (updateMeasure) element.Measure(new Size(300, double.PositiveInfinity));
            var height = (int)Math.Ceiling(element.DesiredSize.Height);

            if (height <= 0 || height == _lastResizedHeight) return;
            _lastResizedHeight = height;

            Width = 400;
            Height = height + 10;
        }
        finally { _isResizing = false; }
    }

    private void OnRootElementLoaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;

        ResizeWindowToContent(element, true);
        //Activate();

        element.LayoutUpdated += (_, _) => ResizeWindowToContent(element, false);
    }

    private void OnServiceToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggle = sender as ToggleSwitch;
        if (toggle == null) return;

        var isOn = toggle.IsOn;
        App.SetClipboardRecording(isOn);
    }

    public void RefreshLocalizedContent()
    {
        var resourceLoader = new ResourceLoader();

        Title = resourceLoader.GetString("AppDisplayName");
        AppTitleBar.Title = resourceLoader.GetString("AppTitleBar/Title");
        TaskbarIcon.ToolTipText = resourceLoader.GetString("TaskbarIcon/ToolTipText");
        AuthorMenuFlyoutItem.Text = resourceLoader.GetString("AuthorMenuFlyoutItem/Text");
        OpenSettingsMenuFlyoutItem.Text = resourceLoader.GetString("OpenSettingsMenuFlyoutItem/Text");
        LanguageMenuBarItem.Title = resourceLoader.GetString("LanguageMenuBarItem/Title");
        HelpMenuBarItem.Title = resourceLoader.GetString("HelpMenuBarItem/Title");
        EnglishLanguageRadioMenuFlyoutItem.Text = resourceLoader.GetString("EnglishLanguageRadioMenuFlyoutItem/Text");
        KoreanLanguageRadioMenuFlyoutItem.Text = resourceLoader.GetString("KoreanLanguageRadioMenuFlyoutItem/Text");
        JapaneseLanguageRadioMenuFlyoutItem.Text = resourceLoader.GetString("JapaneseLanguageRadioMenuFlyoutItem/Text");
        ChineseSimplifiedLanguageRadioMenuFlyoutItem.Text = resourceLoader.GetString("ChineseSimplifiedLanguageRadioMenuFlyoutItem/Text");
        ChineseTraditionalLanguageRadioMenuFlyoutItem.Text = resourceLoader.GetString("ChineseTraditionalLanguageRadioMenuFlyoutItem/Text");
        CheckForUpdatesMenuFlyoutItem.Text = resourceLoader.GetString("CheckForUpdatesMenuFlyoutItem/Text");
        CreatorGitHubMenuFlyoutItem.Text = resourceLoader.GetString("CreatorGitHubMenuFlyoutItem/Text");
        CloseMenuFlyoutItem.Text = resourceLoader.GetString("CloseMenuFlyoutItem/Text");
        ServiceToggleSwitch.OnContent = resourceLoader.GetString("ServiceToggleSwitch/OnContent");
        ServiceToggleSwitch.OffContent = resourceLoader.GetString("ServiceToggleSwitch/OffContent");

        if (AppFrame.Content is MainPage mainPage) mainPage.RefreshLocalizedContent();
        else AppFrame.Navigate(typeof(MainPage));

        UpdateSelectedLanguageMenuItems();
    }

    private nint WindowSubclassProc(nint windowHandle, uint message, nint windowParameter, nint longParameter, nuint subclassIdentifier, nuint referenceData)
    {
        switch (message)
        {
            case WindowMessageQueryEndSession:
                _systemShutdown = true;
                return 1;

            case WindowMessageEndSession:
                if (windowParameter != 0) Environment.Exit(0);
                return 0;

            case WindowMessageClose:
                if (_forceClose || _systemShutdown)
                    break;
                this.Hide();
                return 0;
        }

        return DefSubclassProc(windowHandle, message, windowParameter, longParameter);
    }

    [RelayCommand]
    private void OpenSettingsWindow() => App.ShowSettingsWindow();

    private void OnLanguageRadioMenuFlyoutItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioMenuFlyoutItem radioMenuFlyoutItem) return;
        if (radioMenuFlyoutItem.Tag is not string languageTag) return;
        if (Configuration.LanguageTag == languageTag)
        {
            UpdateSelectedLanguageMenuItems();
            return;
        }

        App.SetLanguage(languageTag);
    }

    private async void OnCheckForUpdatesMenuFlyoutItemClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        var resourceLoader = new ResourceLoader();
        var hasStoreUpdate = false;
        var currentPackageVersion = string.Empty;
        Exception caughtException = null;

        try
        {
            ShowLoading(resourceLoader.GetString("UpdateCheckDialog/Title"));
            hasStoreUpdate = await UpdateCheckManager.HasStoreUpdateAsync();
            if (!hasStoreUpdate) currentPackageVersion = UpdateCheckManager.GetCurrentPackageVersion();
        }
        catch (Exception exception)
        {
            caughtException = exception;
        }
        finally
        {
            HideLoading();
        }

        if (caughtException != null)
        {
            var contentDialogResult = await ShowContentDialogAsync(
                resourceLoader.GetString("UpdateCheckDialog/Title"),
                string.Format(resourceLoader.GetString("UpdateCheckFailedDialog/Content"), Environment.NewLine, caughtException.Message),
                resourceLoader.GetString("UpdateAvailableDialog/PrimaryButtonText"),
                resourceLoader.GetString("DialogCloseButtonText"));

            if (contentDialogResult == ContentDialogResult.Primary)
                await TryOpenStorePageAsync(resourceLoader);
            return;
        }

        if (hasStoreUpdate)
        {
            var contentDialogResult = await ShowContentDialogAsync(
                resourceLoader.GetString("UpdateCheckDialog/Title"),
                resourceLoader.GetString("UpdateAvailableDialog/Content"),
                resourceLoader.GetString("UpdateAvailableDialog/PrimaryButtonText"),
                resourceLoader.GetString("DialogCloseButtonText"));

            if (contentDialogResult == ContentDialogResult.Primary)
                await TryOpenStorePageAsync(resourceLoader);
            return;
        }

        await ShowContentDialogAsync(
            resourceLoader.GetString("UpdateCheckDialog/Title"),
            string.Format(resourceLoader.GetString("UpdateLatestDialog/Content"), currentPackageVersion),
            string.Empty,
            resourceLoader.GetString("DialogCloseButtonText"));
    }

    private async void OnCreatorGitHubMenuFlyoutItemClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        var resourceLoader = new ResourceLoader();
        if (await UpdateCheckManager.OpenCreatorGitHubRepositoryAsync()) return;

        await ShowContentDialogAsync(
            resourceLoader.GetString("CreatorGitHubDialog/Title"),
            resourceLoader.GetString("CreatorGitHubDialog/Content"),
            string.Empty,
            resourceLoader.GetString("DialogCloseButtonText"));
    }

    private void OnCloseProgramMenuFlyoutItemClicked(XamlUICommand sender, ExecuteRequestedEventArgs args) => Environment.Exit(0);

    private void UpdateSelectedLanguageMenuItems()
    {
        var selectedLanguageTag = Configuration.LanguageTag;

        EnglishLanguageRadioMenuFlyoutItem.IsChecked = selectedLanguageTag == "en-US";
        KoreanLanguageRadioMenuFlyoutItem.IsChecked = selectedLanguageTag == "ko-KR";
        JapaneseLanguageRadioMenuFlyoutItem.IsChecked = selectedLanguageTag == "ja-JP";
        ChineseSimplifiedLanguageRadioMenuFlyoutItem.IsChecked = selectedLanguageTag == "zh-Hans";
        ChineseTraditionalLanguageRadioMenuFlyoutItem.IsChecked = selectedLanguageTag == "zh-Hant";
    }

    private async Task<ContentDialogResult> ShowContentDialogAsync(string title, string content, string primaryButtonText, string closeButtonText)
    {
        if (Content is not FrameworkElement rootElement)
            throw new InvalidOperationException("The window content root element is unavailable.");

        var contentDialog = new ContentDialog
        {
            XamlRoot = rootElement.XamlRoot,
            Title = title,
            Content = content,
            CloseButtonText = closeButtonText
        };

        if (!string.IsNullOrWhiteSpace(primaryButtonText))
        {
            contentDialog.PrimaryButtonText = primaryButtonText;
            contentDialog.DefaultButton = ContentDialogButton.Primary;
        }

        return await contentDialog.ShowAsync();
    }

    private async Task TryOpenStorePageAsync(ResourceLoader resourceLoader)
    {
        if (await UpdateCheckManager.OpenStorePageAsync()) return;

        await ShowContentDialogAsync(
            resourceLoader.GetString("StoreOpenDialog/Title"),
            resourceLoader.GetString("StoreOpenDialog/Content"),
            string.Empty,
            resourceLoader.GetString("DialogCloseButtonText"));
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }
}
