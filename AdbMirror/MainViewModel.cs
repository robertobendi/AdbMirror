using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AdbMirror.Core;
using AdbMirror.Models;

namespace AdbMirror;

/// <summary>
/// UI state controller for the main window. Maps ADB/scrcpy state into simple bindable properties.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly AdbService _adbService = new();
    private readonly ScrcpyService _scrcpyService = new();
    private readonly CancellationTokenSource _pollCts = new();
    private readonly StringBuilder _logBuffer = new();
    private const int MaxLogChars = 10000;

    private string _statusText = "No device connected";
    private string _primaryButtonText = "Mirror";
    private bool _isPrimaryEnabled;
    private ScrcpyPreset _selectedPreset = ScrcpyPreset.Balanced;
    private AndroidDevice? _currentDevice;
    private DeviceState _currentState = DeviceState.NoDevice;
    private bool _isMirroring;
    private string? _lastAutoMirroredSerial;
    private string _logs = "";
    private string _adbPathText = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ScrcpyPreset> Presets { get; } =
        new(new[] { ScrcpyPreset.Low, ScrcpyPreset.Balanced, ScrcpyPreset.High });

    public ICommand PrimaryCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand CopyLogsCommand { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string PrimaryButtonText
    {
        get => _primaryButtonText;
        private set => SetField(ref _primaryButtonText, value);
    }

    public bool IsPrimaryEnabled
    {
        get => _isPrimaryEnabled;
        private set => SetField(ref _isPrimaryEnabled, value);
    }

    public ScrcpyPreset SelectedPreset
    {
        get => _selectedPreset;
        set => SetField(ref _selectedPreset, value);
    }

    public string Logs
    {
        get => _logs;
        private set => SetField(ref _logs, value);
    }

    public string AdbPathText
    {
        get => _adbPathText;
        private set => SetField(ref _adbPathText, value);
    }

    private readonly AppSettings _settings = AppSettings.Load();

    public MainViewModel()
    {
        PrimaryCommand = new RelayCommand(_ => OnPrimaryClicked(), _ => IsPrimaryEnabled);
        OpenSettingsCommand = new RelayCommand(_ => ShowSettings());
        CopyLogsCommand = new RelayCommand(_ =>
        {
            try
            {
                Clipboard.SetText(Logs);
                Log("Logs copied to clipboard");
            }
            catch (Exception ex)
            {
                Log($"Failed to copy logs: {ex.Message}");
            }
        });

        // Apply settings
        SelectedPreset = _settings.DefaultPreset;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        Log("Initializing Phone Mirror...");
        
        if (!_adbService.IsAdbAvailable(out var adbError))
        {
            Log($"ADB check failed: {adbError ?? "Unknown error"}");
            StatusText = "ADB not found. Please install platform-tools or configure bundled adb.";
            IsPrimaryEnabled = false;
            return;
        }

        Log("ADB is available");
        AdbPathText = $"Using ADB: {_adbService.GetAdbPath()}";

        if (!_scrcpyService.IsScrcpyAvailable(out var scrcpyError))
        {
            Log($"scrcpy check failed: {scrcpyError ?? "Not found"}");
            StatusText = "scrcpy not found. Please bundle scrcpy or add it to PATH.";
            IsPrimaryEnabled = false;
        }
        else
        {
            Log("scrcpy is available");
        }

        Log("Starting device polling...");
        await _adbService.StartPollingAsync(TimeSpan.FromSeconds(1), OnDeviceStateChanged, _pollCts.Token)
            .ConfigureAwait(false);
    }

    private void OnDeviceStateChanged(DeviceState state, AndroidDevice? device)
    {
        _currentState = state;
        _currentDevice = device;

        var deviceInfo = device != null ? $"{FormatDevice(device)} ({device.Serial})" : "none";
        Log($"Device state changed: {state}, Device: {deviceInfo}");

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_isMirroring)
            {
                StatusText = "Mirroring running";
                PrimaryButtonText = "Stop";
                IsPrimaryEnabled = true;
                return;
            }

            switch (state)
            {
                case DeviceState.AdbNotAvailable:
                    StatusText = "ADB not available. Check installation.";
                    IsPrimaryEnabled = false;
                    break;
                case DeviceState.NoDevice:
                    StatusText = "No device connected";
                    IsPrimaryEnabled = false;
                    _lastAutoMirroredSerial = null;
                    break;
                case DeviceState.Unauthorized:
                    StatusText = "Device unauthorized: check phone prompt";
                    IsPrimaryEnabled = false;
                    break;
                case DeviceState.Offline:
                    StatusText = "Device offline";
                    IsPrimaryEnabled = false;
                    _lastAutoMirroredSerial = null;
                    break;
                case DeviceState.MultipleDevices:
                    StatusText = device is null
                        ? "Multiple devices connected"
                        : $"Multiple devices; using {FormatDevice(device)}";
                    IsPrimaryEnabled = true;
                    // Do not auto-mirror when multiple devices are present to avoid ambiguity.
                    break;
                case DeviceState.Connected:
                    StatusText = device is null
                        ? "Device connected"
                        : $"Device connected: {FormatDevice(device)}";
                    IsPrimaryEnabled = true;

                    if (_settings.AutoMirrorOnConnect
                        && !_isMirroring
                        && device is not null
                        && !string.Equals(_lastAutoMirroredSerial, device.Serial, StringComparison.Ordinal))
                    {
                        // Fire and forget; keep UI responsive.
                        // Guarded so we only auto-start once per device connection.
                        _lastAutoMirroredSerial = device.Serial;
                        OnPrimaryClicked();
                    }

                    break;
                default:
                    StatusText = "Unknown device state";
                    IsPrimaryEnabled = false;
                    break;
            }

            PrimaryButtonText = "Mirror";
        });
    }

    private static string FormatDevice(AndroidDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.Model))
        {
            return device.Model;
        }

        return device.Serial;
    }

    private void OnPrimaryClicked()
    {
        if (_isMirroring)
        {
            Log("Stopping scrcpy...");
            _scrcpyService.StopMirroring();
            _isMirroring = false;
            Log("scrcpy stopped");
            OnDeviceStateChanged(_currentState, _currentDevice);
            return;
        }

        if (_currentDevice == null || _currentState != DeviceState.Connected && _currentState != DeviceState.MultipleDevices)
        {
            return;
        }

        Log($"Starting scrcpy for device {_currentDevice.Serial} with preset {SelectedPreset}");
        if (!_scrcpyService.StartMirroring(_currentDevice.Serial, SelectedPreset, OnScrcpyExited, out var error))
        {
            Log($"Failed to start scrcpy: {error ?? "Unknown error"}");
            StatusText = string.IsNullOrWhiteSpace(error)
                ? "Failed to start scrcpy."
                : error!;
            return;
        }

        Log("scrcpy started successfully");
        _isMirroring = true;
        StatusText = "Mirroring running";
        PrimaryButtonText = "Stop";
        IsPrimaryEnabled = true;
    }

    private void OnScrcpyExited(string message)
    {
        Log($"scrcpy exited: {message}");
        _isMirroring = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = message;
            PrimaryButtonText = "Mirror";
            OnDeviceStateChanged(_currentState, _currentDevice);
        });
    }

    private void ShowSettings()
    {
        var window = new SettingsWindow(_settings)
        {
            Owner = Application.Current.MainWindow
        };

        var result = window.ShowDialog();
        if (result == true)
        {
            // Re-read effective settings
            SelectedPreset = _settings.DefaultPreset;
        }
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logLine = $"[{timestamp}] {message}";
        _logBuffer.AppendLine(logLine);
        
        // Keep buffer size manageable
        if (_logBuffer.Length > MaxLogChars)
        {
            var excess = _logBuffer.Length - MaxLogChars;
            _logBuffer.Remove(0, excess);
        }
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            Logs = _logBuffer.ToString();
        });
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _pollCts.Cancel();
        _pollCts.Dispose();
        _scrcpyService.StopMirroring();
    }
}

/// <summary>
/// Lightweight ICommand helper for simple bindings.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<object?> _execute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}


