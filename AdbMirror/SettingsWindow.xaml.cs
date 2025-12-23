using System.Collections.ObjectModel;
using System.Windows;
using AdbMirror.Core;

namespace AdbMirror;

public partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        ViewModel = new SettingsViewModel(settings);
        DataContext = ViewModel;
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyAndSave();
        DialogResult = true;
        Close();
    }
}

public sealed class SettingsViewModel
{
    private readonly AppSettings _settings;

    public ObservableCollection<ScrcpyPreset> Presets { get; } =
        new(new[] { ScrcpyPreset.Low, ScrcpyPreset.Balanced, ScrcpyPreset.High });

    public ScrcpyPreset DefaultPreset { get; set; }
    public bool AutoMirrorOnConnect { get; set; }
    public bool StartFullscreen { get; set; }

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        DefaultPreset = settings.DefaultPreset;
        AutoMirrorOnConnect = settings.AutoMirrorOnConnect;
        StartFullscreen = settings.StartFullscreen;
    }

    public void ApplyAndSave()
    {
        _settings.DefaultPreset = DefaultPreset;
        _settings.AutoMirrorOnConnect = AutoMirrorOnConnect;
        _settings.StartFullscreen = StartFullscreen;
        _settings.Save();
    }
}



