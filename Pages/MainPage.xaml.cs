using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Linq;
using WinRT.Interop;

namespace AutoClipboardSaver.Pages;

public sealed partial class MainPage : Page
{
    private bool _isInitializing;

    public MainPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        RefreshLocalizedContent();

        SaveDirectoryPathTextBox.Text = Configuration.SaveDirectoryPath;
        SaveWithTimestampToggleSwitch.IsOn = Configuration.SaveWithTimestamp;
        MaxImagesComboBox.IsEnabled = Configuration.SaveWithTimestamp;
        LaunchOnStartupCheckBox.IsChecked = Configuration.LaunchOnStartup;

        var saveFileFormat = Configuration.SaveFileFormat;
        foreach (ComboBoxItem item in FileFormatComboBox.Items.Cast<ComboBoxItem>())
        {
            if ((string)item.Tag == saveFileFormat)
            {
                FileFormatComboBox.SelectedItem = item;
                break;
            }
        }

        var maximumImages = Configuration.MaxImages;
        foreach (ComboBoxItem item in MaxImagesComboBox.Items.Cast<ComboBoxItem>())
        {
            if (int.TryParse((string)item.Tag, out int tag) && tag == maximumImages)
            {
                MaxImagesComboBox.SelectedItem = item;
                break;
            }
        }

        ExpirationComboBox.IsEnabled = Configuration.SaveWithTimestamp;
        var expirationMinutes = Configuration.ExpirationMinutes;
        foreach (ComboBoxItem item in ExpirationComboBox.Items.Cast<ComboBoxItem>())
        {
            if (int.TryParse((string)item.Tag, out int tag) && tag == expirationMinutes)
            {
                ExpirationComboBox.SelectedItem = item;
                break;
            }
        }

        _isInitializing = false;
    }

    public void RefreshLocalizedContent()
    {
        var resourceLoader = new ResourceLoader();

        SaveDirectoryPathTextBox.Header = resourceLoader.GetString("SaveDirectoryPathTextBox/Header");
        SaveWithTimestampToggleSwitch.OnContent = resourceLoader.GetString("SaveWithTimestampToggleSwitch/OnContent");
        SaveWithTimestampToggleSwitch.OffContent = resourceLoader.GetString("SaveWithTimestampToggleSwitch/OffContent");
        FileFormatComboBox.Header = resourceLoader.GetString("FileFormatComboBox/Header");
        MaxImagesComboBox.Header = resourceLoader.GetString("MaxImagesComboBox/Header");
        UnlimitedComboBoxItem.Content = resourceLoader.GetString("UnlimitedComboBoxItem/Content");
        ExpirationComboBox.Header = resourceLoader.GetString("ExpirationComboBox/Header");
        ExpirationDisabledComboBoxItem.Content = resourceLoader.GetString("ExpirationDisabledComboBoxItem/Content");
        LaunchOnStartupCheckBox.Content = resourceLoader.GetString("LaunchOnStartupCheckBox/Content");
    }

    private async void OnBrowseFolderButtonClicked(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder == null) return;

        Configuration.SaveDirectoryPath = folder.Path;
        SaveDirectoryPathTextBox.Text = folder.Path;
    }

    private void OnSaveWithTimestampToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        Configuration.SaveWithTimestamp = SaveWithTimestampToggleSwitch.IsOn;
        MaxImagesComboBox.IsEnabled = SaveWithTimestampToggleSwitch.IsOn;
        ExpirationComboBox.IsEnabled = SaveWithTimestampToggleSwitch.IsOn;
    }

    private void OnFileFormatComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (FileFormatComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
        Configuration.SaveFileFormat = (string)selectedItem.Tag;
    }

    private void OnMaxImagesComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (MaxImagesComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
        if (int.TryParse((string)selectedItem.Tag, out int maximumImages))
            Configuration.MaxImages = maximumImages;
    }

    private void OnExpirationComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (ExpirationComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
        if (int.TryParse((string)selectedItem.Tag, out int expirationMinutes))
            Configuration.ExpirationMinutes = expirationMinutes;
    }

    private void OnLaunchOnStartupCheckBoxClicked(object sender, RoutedEventArgs e) =>
        Configuration.LaunchOnStartup = LaunchOnStartupCheckBox.IsChecked ?? false;
}
