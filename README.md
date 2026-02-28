# PasMan

PasMan is a desktop password manager built with Avalonia UI and .NET.
It stores entries in an encrypted local vault file.

## Features

- Local encrypted vault (`vault.bin`) with master-password unlock.
- Add password entries.
- Save (update) selected password entry.
- Delete password entries.
- Delete confirmation dialog to prevent accidental removal.
- Search entries by `Title`, `Login`, `Url`, or `Note`.
- Edit fields of selected entry in the right panel.

## Security and Storage

Vault persistence is handled by `VaultManager`.

- Encryption: AES-GCM
- Key derivation: PBKDF2-SHA256 (`200_000` iterations)
- Atomic save flow: write temp file, flush, replace original

Vault file path is based on `Environment.SpecialFolder.ApplicationData`:

- Linux example: `/home/<user>/.config/PasMan/vault.bin`
- Windows example: `C:\Users\<user>\AppData\Roaming\PasMan\vault.bin`

## Requirements

- .NET SDK 10.0+
- Linux, Windows, or macOS

## Build and Run

From repository root:

```bash
dotnet restore PasMan.sln
dotnet run --project PasManApp/PasManApp.csproj
```

If your environment blocks Avalonia telemetry file writes, build with:

```bash
dotnet build PasManApp/PasManApp.csproj /p:UsedAvaloniaProducts=
```

## Usage

1. Start app.
2. Enter master password in unlock dialog.
3. If password is wrong for existing vault, app closes.
4. Add new entry from left panel.
5. Select an entry to view/edit details.
6. Click `Save` to persist changes to selected entry.
7. Click `Delete` and confirm in dialog to remove selected entry.
8. Use Search box for live filtering.

## Project Structure

- `PasManApp/Views` - Avalonia views (`MainWindow`, `MasterPasswordWindow`)
- `PasManApp/ViewModels` - MVVM logic and commands
- `PasManApp/Services/VaultManager.cs` - encrypted vault operations
- `PasManApp/Models/PasswordEntry.cs` - password entry model
- `PasManAppTests` - test project

## Current Notes
- On first run (no existing vault file), any master password creates a new empty vault.
