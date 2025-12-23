namespace AdbMirror.Core;

/// <summary>
/// High-level device state as understood by the UI.
/// </summary>
public enum DeviceState
{
    NoDevice,
    Unauthorized,
    Offline,
    Connected,
    MultipleDevices,
    AdbNotAvailable,
    ScrcpyNotAvailable,
    Mirroring
}


