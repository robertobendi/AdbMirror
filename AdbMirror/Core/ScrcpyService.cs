using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net.Http;
using System.IO.Compression;

namespace AdbMirror.Core;

/// <summary>
/// Starts and controls a scrcpy process for mirroring a given device.
/// </summary>
public sealed class ScrcpyService
{
    private string _scrcpyPath;
    private Process? _currentProcess;

    public ScrcpyService(string? scrcpyPath = null)
    {
        _scrcpyPath = ResolveScrcpyPath(scrcpyPath);
    }

    /// <summary>
    /// Determines if scrcpy is available and returns the first error line if not.
    /// </summary>
    public bool IsScrcpyAvailable(out string? error)
    {
        if (File.Exists(_scrcpyPath))
        {
            error = null;
            return true;
        }

        // Try to resolve again in case PATH or folders changed
        _scrcpyPath = ResolveScrcpyPath(_scrcpyPath);
        if (File.Exists(_scrcpyPath))
        {
            error = null;
            return true;
        }

        error = $"scrcpy not found at '{_scrcpyPath}'.\n" +
                "Place scrcpy.exe in a 'scrcpy' folder next to this app, or install it and add it to PATH.";
        return false;
    }

    private static string ResolveScrcpyPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return explicitPath;
        }

        var candidates = new List<string>();
        
        // Check embedded resources first (extracted to temp)
        var extractedScrcpy = ResourceExtractor.GetScrcpyPath();
        if (!string.IsNullOrEmpty(extractedScrcpy) && File.Exists(extractedScrcpy))
        {
            candidates.Add(extractedScrcpy);
        }

        var baseDir = AppContext.BaseDirectory;
        
        // Check bundled locations
        candidates.Add(Path.Combine(baseDir, "scrcpy", "scrcpy.exe"));
        candidates.Add(Path.Combine(baseDir, "scrcpy.exe"));
        
        // Check parent directories (for when running from bin\Debug\net8.0-windows)
        var currentDir = Directory.GetParent(baseDir);
        for (int i = 0; i < 5 && currentDir != null; i++)
        {
            candidates.Add(Path.Combine(currentDir.FullName, "scrcpy", "scrcpy.exe"));
            currentDir = currentDir.Parent;
        }
        
        // Check PATH directories
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathParts = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawDir in pathParts)
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(dir)) continue;
            candidates.Add(Path.Combine(dir, "scrcpy.exe"));
        }
        
        // Prefer the first existing candidate
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // As a last resort, attempt to bootstrap scrcpy into a local folder.
        var bootstrapped = TryBootstrapScrcpy(out var localPath);
        if (bootstrapped && localPath is not null && File.Exists(localPath))
        {
            return localPath;
        }

        // Fall back to PATH; if missing the process start will fail and be surfaced via UI.
        return "scrcpy";
    }

    /// <summary>
    /// Attempts to download and extract scrcpy into a local 'scrcpy' folder next to the app.
    /// This is a best-effort helper to avoid manual setup; failures are surfaced via the caller.
    /// </summary>
    private static bool TryBootstrapScrcpy(out string? scrcpyPath)
    {
        scrcpyPath = null;
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var targetDir = Path.Combine(baseDir, "scrcpy");
            Directory.CreateDirectory(targetDir);

            // If it already exists, don't download again.
            var existingExe = Path.Combine(targetDir, "scrcpy.exe");
            if (File.Exists(existingExe))
            {
                scrcpyPath = existingExe;
                return true;
            }

            const string url = "https://github.com/Genymobile/scrcpy/releases/download/v3.1/scrcpy-win64-v3.1.zip";
            var tempZip = Path.Combine(Path.GetTempPath(), "scrcpy-win64.zip");

            using (var httpClient = new HttpClient())
            using (var response = httpClient.GetAsync(url).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                using var zipStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                using var fileStream = File.Create(tempZip);
                zipStream.CopyTo(fileStream);
            }

            // Extract into a temporary folder first
            var tempExtract = Path.Combine(Path.GetTempPath(), "scrcpy-extract");
            if (Directory.Exists(tempExtract))
            {
                Directory.Delete(tempExtract, true);
            }

            ZipFile.ExtractToDirectory(tempZip, tempExtract, true);
            File.Delete(tempZip);

            // Find the first directory that contains scrcpy.exe
            string? foundExe = null;
            foreach (var dir in Directory.GetDirectories(tempExtract, "*", SearchOption.AllDirectories))
            {
                var candidate = Path.Combine(dir, "scrcpy.exe");
                if (File.Exists(candidate))
                {
                    foundExe = candidate;
                    break;
                }
            }

            if (foundExe == null)
            {
                // Try root of extract
                var rootCandidate = Path.Combine(tempExtract, "scrcpy.exe");
                if (File.Exists(rootCandidate))
                {
                    foundExe = rootCandidate;
                }
            }

            if (foundExe == null)
            {
                // Cleanup and fail
                Directory.Delete(tempExtract, true);
                return false;
            }

            // Copy entire containing directory into targetDir
            var sourceDir = Path.GetDirectoryName(foundExe) ?? tempExtract;
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var destFile = Path.Combine(targetDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);
            }

            Directory.Delete(tempExtract, true);

            scrcpyPath = Path.Combine(targetDir, "scrcpy.exe");
            return File.Exists(scrcpyPath);
        }
        catch
        {
            // Silent failure; caller will handle missing scrcpy.
            scrcpyPath = null;
            return false;
        }
    }

    /// <summary>
    /// Starts scrcpy with the given preset and device serial. Returns false if it fails immediately.
    /// </summary>
    public bool StartMirroring(string serial, ScrcpyPreset preset, Action<string>? exitedCallback, out string? error)
    {
        StopMirroring();

        if (!IsScrcpyAvailable(out error))
        {
            return false;
        }

        var args = BuildArguments(serial, preset);
        var psi = new ProcessStartInfo
        {
            FileName = _scrcpyPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(_scrcpyPath)) ?? AppContext.BaseDirectory
        };

        try
        {
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Exited += (_, _) =>
            {
                string? stdErr = null;
                string? stdOut = null;
                try
                {
                    stdErr = process.StandardError.ReadToEnd();
                    stdOut = process.StandardOutput.ReadToEnd();
                }
                catch
                {
                    // ignore read errors
                }

                var builder = new StringBuilder();
                builder.Append(process.ExitCode == 0
                    ? "scrcpy exited."
                    : $"scrcpy failed with code {process.ExitCode}.");

                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    builder.Append(" stderr: ").Append(stdErr.Trim());
                }
                else if (!string.IsNullOrWhiteSpace(stdOut))
                {
                    builder.Append(" stdout: ").Append(stdOut.Trim());
                }

                exitedCallback?.Invoke(builder.ToString());
                process.Dispose();
            };

            var started = process.Start();
            if (!started)
            {
                error = "Failed to start scrcpy process.";
                process.Dispose();
                return false;
            }

            _currentProcess = process;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to start scrcpy: {ex.Message}\nPath: {_scrcpyPath}\nArgs: {args}";
            return false;
        }
    }

    /// <summary>
    /// Requests that the active scrcpy process stop.
    /// </summary>
    public void StopMirroring()
    {
        if (_currentProcess == null)
        {
            return;
        }

        try
        {
            if (!_currentProcess.HasExited)
            {
                _currentProcess.Kill();
            }
        }
        catch
        {
            // Ignore kill failures; process may already be gone.
        }
        finally
        {
            _currentProcess.Dispose();
            _currentProcess = null;
        }
    }

    private static string BuildArguments(string serial, ScrcpyPreset preset)
    {
        var builder = new StringBuilder();
        builder.Append("-s ").Append('"').Append(serial).Append('"');

        switch (preset)
        {
            case ScrcpyPreset.Low:
                builder.Append(" --video-bit-rate 4M --max-size 1024 --max-fps 30");
                break;
            case ScrcpyPreset.Balanced:
                builder.Append(" --video-bit-rate 8M --max-size 1280 --max-fps 60");
                break;
            case ScrcpyPreset.High:
                builder.Append(" --video-bit-rate 16M --max-size 1920 --max-fps 60");
                break;
        }

        // Sensible defaults for mirroring use
        //
        // NOTE:
        //  - `--turn-screen-off` requires control to be enabled; combining it with `--no-control`
        //    causes scrcpy to fail immediately with:
        //      "ERROR: Cannot request to turn screen off if control is disabled"
        //  - To keep a simple out-of-the-box experience where mirroring “just works”,
        //    we enable control and still request the device screen to turn off.
        //
        // If a “view-only” mode is needed later, it should be exposed as an explicit option that
        // omits `--turn-screen-off` when `--no-control` is requested.
        builder.Append(" --stay-awake --turn-screen-off");
        return builder.ToString();
    }
}

public enum ScrcpyPreset
{
    Low,
    Balanced,
    High
}


