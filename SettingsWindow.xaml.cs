using AutoClipboardSaver.Pages;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using Windows.Foundation;
using WinUIEx;

namespace AutoClipboardSaver;

public sealed partial class SettingsWindow : WindowEx
{
    private int _lastResizedHeight;
    private bool _isResizing;

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

    private void OnClosed(object sender, WindowEventArgs args)
    {
        args.Handled = true;
        this.Hide();
    }

    [RelayCommand]
    private void OpenSettingsWindow() => App.ShowSettingsWindow();
    private void OnCloseProgramMenuFlyoutItemClicked(XamlUICommand sender, ExecuteRequestedEventArgs args) => Environment.Exit(0);
}
