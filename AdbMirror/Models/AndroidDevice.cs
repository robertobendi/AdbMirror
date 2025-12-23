namespace AdbMirror.Models;

/// <summary>
/// Simple representation of an Android device as reported by ADB.
/// </summary>
public sealed class AndroidDevice
{
    public string Serial { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string StateRaw { get; init; } = string.Empty;
}


