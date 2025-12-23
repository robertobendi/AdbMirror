# AdbMirror v1.0.0

**Phone Mirror** — A streamlined Windows desktop application for live Android device mirroring via ADB and scrcpy.

## 🎯 What's New

First public release of AdbMirror! A clean, focused tool for mirroring your Android device's screen to your Windows PC in real-time.

## ✨ Features

- **📱 Live Screen Mirroring**: Real-time Android device screen mirroring using scrcpy
- **🔍 Automatic Device Detection**: Continuously monitors for connected Android devices via ADB
- **⚙️ Quality Presets**: Choose from Low, Balanced, or High quality presets for optimal performance
- **🚀 Auto-Mirror**: Optionally start mirroring automatically when a device connects
- **📊 Device Status Monitoring**: Real-time status updates showing device connection state
- **📝 Event Logging**: Detailed event log with timestamps for troubleshooting
- **💾 Settings Persistence**: Saves your preferences (default preset, auto-mirror, fullscreen options)
- **🌙 Dark UI**: Beautiful dark theme following the Labyrica design language
- **📦 Single-File Executable**: All dependencies embedded - just one exe file!

## 📥 Download

**Windows x64** - [Download AdbMirror.exe](https://github.com/YOUR_USERNAME/AdbMirror/releases/download/v1.0.0/AdbMirror.exe)

> **Note**: The executable is self-contained and includes all dependencies (ADB platform-tools and scrcpy). No installation required - just download and run!

## 📋 System Requirements

- **OS**: Windows 10/11 (64-bit)
- **.NET Runtime**: Not required - included in the executable
- **Android Device**: USB debugging enabled

## 🚀 Quick Start

1. **Download** `AdbMirror.exe` from the releases page
2. **Connect** your Android device via USB
3. **Enable USB debugging** on your Android device (Settings → Developer Options)
4. **Authorize** the computer when prompted on your phone
5. **Launch** `AdbMirror.exe`
6. **Select** your desired Quality Preset (Low/Balanced/High)
7. **Click Mirror** to start screen mirroring!

## 🎮 Usage

### Main Interface

- **Left Panel**: Shows device status, ADB path information, and event logs
- **Right Panel**: Device controls, mirror button, quality preset selector, and settings

### Quality Presets

- **Low**: Lower resolution/bitrate for better performance on slower connections
- **Balanced**: Default preset with good balance of quality and performance  
- **High**: Higher resolution/bitrate for best visual quality

### Settings

- **Auto mirror when device connects**: Automatically start mirroring when a device is detected
- **Start scrcpy in fullscreen**: Launch the mirror window in fullscreen mode
- **Keep screen awake**: Prevent Android device from sleeping during mirroring

### Event Log

The event log shows:
- Device connection/disconnection events
- ADB and scrcpy status messages
- Error messages and troubleshooting information
- Use the copy button to copy logs to clipboard

## 🛠️ Building from Source

If you want to build from source:

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/AdbMirror.git
cd AdbMirror

# Build and run
build.cmd

# Or publish a release build
publish.cmd
```

**Requirements for building:**
- .NET SDK 8.0
- Windows 10/11
- `platform-tools` folder with ADB
- `scrcpy` folder with scrcpy executable

## 🐛 Troubleshooting

### "ADB not found"
- The bundled ADB should work automatically
- If issues persist, ensure USB debugging is enabled on your device

### "scrcpy not found"  
- The bundled scrcpy should work automatically
- Check the event log for specific error messages

### Device not detected
- Enable USB debugging on your Android device (Settings → Developer Options)
- Authorize the computer when prompted
- Check USB cable connection
- Try a different USB port or cable

### Mirroring fails to start
- Check event log for specific error messages
- Ensure device is in "Connected" state (not "Unauthorized" or "Offline")
- Try a different quality preset

## 📝 Changelog

### v1.0.0 (Initial Release)
- First public release
- Single-file executable with embedded dependencies
- Dark UI theme
- Quality presets (Low/Balanced/High)
- Auto-mirror on device connect
- Fullscreen support
- Keep screen awake option
- Event logging with copy to clipboard
- Settings persistence

## 📄 License

See LICENSE file for details.

## 🙏 Credits

Built by [Labyrica](https://labyrica.com) — Data driven solutions.

**Open Source Components:**
- [scrcpy](https://github.com/Genymobile/scrcpy) - Screen mirroring engine
- [ADB Platform Tools](https://developer.android.com/tools/releases/platform-tools) - Android Debug Bridge

---

**Enjoy mirroring your Android device!** 🎉

If you encounter any issues, please [open an issue](https://github.com/YOUR_USERNAME/AdbMirror/issues) on GitHub.

