using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdbMirror.Models;

namespace AdbMirror.Core;

/// <summary>
/// Thin wrapper around the adb executable: device discovery, state and server management.
/// </summary>
public sealed class AdbService
{
    private readonly string _adbPath;

    public AdbService(string? adbPath = null)
    {
        _adbPath = ResolveAdbPath(adbPath);
    }

    /// <summary>
    /// Returns the resolved ADB path for display purposes.
    /// </summary>
    public string GetAdbPath() => _adbPath;

    /// <summary>
    /// Attempts to locate adb in a user-friendly way:
    /// 1) An explicit path (if provided)
    /// 2) Bundled locations near the application
    /// 3) Common Android SDK locations (ANDROID_HOME / ANDROID_SDK_ROOT / LocalAppData)
    /// 4) Directories on PATH (plus the result of `where adb`)
    /// 5) Finally, the bare command name "adb" (so any remaining PATH resolution still applies).
    /// </summary>
    private static string ResolveAdbPath(string? explicitPath)
    {
        // 1) Explicit path from configuration
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return explicitPath;
        }

        var candidates = new List<string>();

        // 2) Check embedded resources first (extracted to temp)
        var extractedAdb = ResourceExtractor.GetAdbPath();
        if (!string.IsNullOrEmpty(extractedAdb) && File.Exists(extractedAdb))
        {
            candidates.Add(extractedAdb);
        }

        // 3) Bundled locations relative to the app
        var baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(baseDir, "platform-tools", "adb.exe"));
        candidates.Add(Path.Combine(baseDir, "adb.exe"));
        
        // Check parent directories (for when running from bin\Debug\net8.0-windows)
        var currentDir = Directory.GetParent(baseDir);
        for (int i = 0; i < 5 && currentDir != null; i++) // Go up max 5 levels to reach project root
        {
            candidates.Add(Path.Combine(currentDir.FullName, "platform-tools", "adb.exe"));
            currentDir = currentDir.Parent;
        }

        // 4) Common Android SDK locations
        var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME")
                          ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (!string.IsNullOrWhiteSpace(androidHome))
        {
            candidates.Add(Path.Combine(androidHome, "platform-tools", "adb.exe"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            candidates.Add(Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe"));
        }

        // 5a) Scan PATH directories for adb.exe
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathParts = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawDir in pathParts)
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            var candidate = Path.Combine(dir, "adb.exe");
            candidates.Add(candidate);
        }

        // 5b) Use `where adb` if available for more accurate resolution
        try
        {
            var whereInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "adb",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var whereProcess = Process.Start(whereInfo);
            whereProcess?.WaitForExit(1000);
            if (whereProcess != null && whereProcess.ExitCode == 0)
            {
                var output = whereProcess.StandardOutput.ReadToEnd();
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var path = line.Trim();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        candidates.Add(path);
                    }
                }
            }
        }
        catch
        {
            // If `where` is not available, silently fall back to other strategies.
        }

        // Prefer the first existing candidate
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // 5) Fall back to PATH; let Process fail clearly if not available
        return "adb";
    }

    /// <summary>
    /// Returns the first line of stderr/stdout error if adb cannot be started or is missing.
    /// </summary>
    public bool IsAdbAvailable(out string? error)
    {
        try
        {
            var result = RunAdbCommandRaw("version", TimeSpan.FromSeconds(3));
            if (result.ExitCode == 0)
            {
                error = null;
                return true;
            }

            error = string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Starts the ADB server if necessary.
    /// </summary>
    public void EnsureServerRunning()
    {
        RunAdbCommandRaw("start-server", TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Gets the list of connected devices with their raw state.
    /// </summary>
    public IReadOnlyList<AndroidDevice> GetDevices()
    {
        var result = RunAdbCommandRaw("devices -l", TimeSpan.FromSeconds(5));
        if (result.ExitCode != 0)
        {
            return Array.Empty<AndroidDevice>();
        }

        var devices = new List<AndroidDevice>();
        using var reader = new StringReader(result.Output);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var serial = parts[0];
            var state = parts[1];

            var modelToken = parts.FirstOrDefault(p => p.StartsWith("model:", StringComparison.OrdinalIgnoreCase));
            var model = modelToken?.Substring("model:".Length) ?? string.Empty;

            devices.Add(new AndroidDevice
            {
                Serial = serial,
                Model = model,
                StateRaw = state
            });
        }

        return devices;
    }

    /// <summary>
    /// Computes a high-level device state appropriate for driving the UI.
    /// </summary>
    public DeviceState GetHighLevelState(out AndroidDevice? primaryDevice)
    {
        primaryDevice = null;

        if (!IsAdbAvailable(out _))
        {
            return DeviceState.AdbNotAvailable;
        }

        EnsureServerRunning();

        var devices = GetDevices();
        if (devices.Count == 0)
        {
            return DeviceState.NoDevice;
        }

        if (devices.Count > 1)
        {
            // For v1, just pick the first but expose the state for potential future dropdown
            primaryDevice = devices[0];
            return DeviceState.MultipleDevices;
        }

        primaryDevice = devices[0];
        return primaryDevice.StateRaw switch
        {
            "unauthorized" => DeviceState.Unauthorized,
            "offline" => DeviceState.Offline,
            "device" => DeviceState.Connected,
            _ => DeviceState.Offline
        };
    }

    /// <summary>
    /// Starts a background poll loop that periodically reports the current device state.
    /// </summary>
    public Task StartPollingAsync(TimeSpan interval, Action<DeviceState, AndroidDevice?> observer, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var state = GetHighLevelState(out var device);
                    observer(state, device);
                }
                catch
                {
                    // Keep the poller resilient; errors will surface in high-level state elsewhere.
                }

                try
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, cancellationToken);
    }

    private (int ExitCode, string Output, string Error) RunAdbCommandRaw(string arguments, TimeSpan timeout)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // ignore
            }

            return (-1, string.Empty, "adb command timed out");
        }

        outputTask.Wait(timeout);
        errorTask.Wait(timeout);

        return (process.ExitCode, outputTask.Result, errorTask.Result);
    }
}


