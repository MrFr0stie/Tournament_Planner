# Building a Dartboard installer

Install Inno Setup 6, then run this from the project root:

```powershell
.\tools\Build-Installer.ps1
```

The script publishes a self-contained, single-file Windows build and creates a versioned installer in `installer\Output`, for example `Dartboard-Setup-1.1.0.exe`.

If that setup filename already exists, the script automatically increments the patch version (`1.1.1`, `1.1.2`, and so on). To set a release version explicitly, use:

```powershell
.\tools\Build-Installer.ps1 -Version 1.2.0
```

Every installer uses the same Dartboard application ID. Installing a newer version therefore updates the existing installation in place, closes Dartboard if needed, and retains the local data at `%LOCALAPPDATA%\DartLeague\dart-league.db`.
