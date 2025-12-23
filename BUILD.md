## Build Instructions

### Prerequisites
- **.NET SDK 8.0** (or compatible) installed.
- **Windows** environment (the app targets `net8.0-windows`).

### Easiest: one-click scripts (you run them)

- **CMD**: double‑click `build_and_run.cmd` in the repo root, or run:

```cmd
cd /d C:\Users\rober\Documents\GitHub\AdbMirror
build_and_run.cmd
```

- **PowerShell**:

```powershell
cd C:\Users\rober\Documents\GitHub\AdbMirror
.\build_and_run.ps1
```

Both scripts will:
- cd into `AdbMirror`
- run `dotnet build`
- then `dotnet run` (unless you pass `-NoRun` to the PowerShell script).

### Build and Run from PowerShell (manual)

```powershell
# 1. Go to the project folder
cd C:\Users\rober\Documents\GitHub\AdbMirror\AdbMirror

# 2. (Only if dotnet is not found) Add the SDK to PATH for this session
$env:Path += ";C:\Program Files\dotnet"

# 3. Build the project
dotnet build

# 4. Run the app
dotnet run
```

### Ask the AI assistant to build and run (you tell it what to do)

When you want the assistant to build and start the app for you, send a message like:

```text
Use cmd.exe and run this:
cd /d C:\Users\rober\Documents\GitHub\AdbMirror\AdbMirror && dotnet build && dotnet run
```

The assistant will execute those steps in order: change to the project folder, build the project, and run the app.

### Publishing (Distribution Builds)

**Note**: By default, all `dotnet publish` commands will create a **single-file, self-contained executable** with embedded resources (platform-tools and scrcpy). This is configured in `AdbMirror.csproj` and happens automatically.

To publish:
```cmd
dotnet publish -c Release
```

Or use the convenience scripts:
- `publish_single_file.cmd` (Windows CMD)
- `publish_single_file.ps1` (PowerShell)

The output will be a single `AdbMirror.exe` file with all dependencies embedded.



