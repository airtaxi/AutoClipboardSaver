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

    public SettingsWindow()
    {
        InitializeComponent();

        // Set window icons
        AppWindow.SetIcon("Assets/logo.ico");

        // Title bar customization
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Localized window title
        var resourceLoader = new ResourceLoader();
        Title = resourceLoader.GetString("AppDisplayName");

        // Since Activate() isn't called by default, we can set up the TaskbarIcon commands here
        TaskbarIcon.LeftClickCommand = OpenSettingsWindowCommand;
        OpenSettingsMenuFlyoutItem.Command = OpenSettingsWindowCommand;

        _windowSubclassProcedure = WindowSubclassProc;
        SetWindowSubclass(this.GetWindowHandle(), _windowSubclassProcedure, 1, 0);

        AppFrame.Navigate(typeof(MainPage));
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

    private nint WindowSubclassProc(nint windowHandle, uint message, nint windowParameter, nint longParameter, nuint subclassIdentifier, nuint referenceData)
    {
        switch (message)
        {
            case WindowMessageQueryEndSession:
                _systemShutdown = true;
                return 1;

            case WindowMessageEndSession:
                if (windowParameter != 0) App.Shutdown();
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
    private void OnCloseProgramMenuFlyoutItemClicked(XamlUICommand sender, ExecuteRequestedEventArgs args) => App.Shutdown();
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }
}
