using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace AdbMirror.Core;

/// <summary>
/// Extracts embedded resources (platform-tools and scrcpy) to a temporary directory at runtime.
/// </summary>
public static class ResourceExtractor
{
    private static string? _extractedBasePath;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the base path where resources are extracted. Extracts them on first access.
    /// </summary>
    public static string GetExtractedBasePath()
    {
        if (_extractedBasePath != null && Directory.Exists(_extractedBasePath))
        {
            return _extractedBasePath;
        }

        lock (_lock)
        {
            if (_extractedBasePath != null && Directory.Exists(_extractedBasePath))
            {
                return _extractedBasePath;
            }

            var tempBase = Path.Combine(Path.GetTempPath(), "AdbMirror", Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempBase);

            // Extract platform-tools if embedded
            ExtractEmbeddedResource("platform-tools.zip", Path.Combine(tempBase, "platform-tools"));

            // Extract scrcpy if embedded
            ExtractEmbeddedResource("scrcpy.zip", Path.Combine(tempBase, "scrcpy"));

            _extractedBasePath = tempBase;
            return _extractedBasePath;
        }
    }

    /// <summary>
    /// Gets the path to the extracted adb.exe, or null if not available.
    /// </summary>
    public static string? GetAdbPath()
    {
        try
        {
            var basePath = GetExtractedBasePath();
            var path = Path.Combine(basePath, "platform-tools", "adb.exe");
            return File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the path to the extracted scrcpy.exe, or null if not available.
    /// </summary>
    public static string? GetScrcpyPath()
    {
        try
        {
            var basePath = GetExtractedBasePath();
            var path = Path.Combine(basePath, "scrcpy", "scrcpy.exe");
            return File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ExtractEmbeddedResource(string resourceName, string targetDirectory)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"AdbMirror.Resources.{resourceName}";

            using var stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
            {
                // Resource not embedded, skip silently - app will fall back to external folders
                return;
            }

            Directory.CreateDirectory(targetDirectory);
            
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            zip.ExtractToDirectory(targetDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            // Log but don't throw - app will fall back to external folders or PATH
            System.Diagnostics.Debug.WriteLine($"Failed to extract {resourceName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up extracted resources. Call this on application exit if desired.
    /// </summary>
    public static void Cleanup()
    {
        if (_extractedBasePath != null && Directory.Exists(_extractedBasePath))
        {
            try
            {
                Directory.Delete(_extractedBasePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }
}

