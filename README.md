<p align="center"><img src="assets/short.png" width="96" alt="Short icon"></p>

<h1 align="center">Short</h1>

<p align="center">Turn a deep folder into a drive letter with one right-click. Per-user, no admin rights.</p>

---

## Why

Windows Explorer and most of Office still hit the classic **260-character path limit (MAX_PATH)**. That holds even with `LongPathsEnabled` turned on, because Explorer is not long-path aware. Deep project trees and synced SharePoint/OneDrive libraries regularly cross that limit. Files then fail to open, copy, rename or delete, and the error messages don't say why.

The old fix is `subst R: C:\very\long\path`. The 260-character budget then starts again from `R:\`. **Short** turns that into a right-click that any user can do themselves, and it remembers the mapping across logons.

## Usage

| Where | Right-click → | Result |
|---|---|---|
| Any folder | **Short → Create drive** | A free letter is picked (R:–Z:, then Q:–D:), the folder is mapped to it, and the new drive opens in Explorer |
| A drive created by Short | **Unshort** | The mapping is removed. Folder contents are never touched |

On Windows 11 these items are under **Show more options**.

Mappings are restored automatically at logon. Uninstall from **Settings → Apps** (or *Programs and Features*); this removes all Short mappings, menu entries and the logon entry.

## Behaviour and safety rules

- **Never touches what it didn't create.** Ownership is checked against Short's own record *and* the live mapping. Removal uses `DDD_EXACT_MATCH_ON_REMOVE`, so Windows itself refuses to remove a letter that points elsewhere.
- **Unshort is offered only on Short drives.** The menu entry's `AppliesTo` condition is rewritten after every change.
- **No chained mappings.** If you pick a folder *through* a Short drive (`R:\sub`), the new letter is mapped to the real path. Short drives never depend on each other.
- **Same folder twice.** You get the existing letter, not a new one.
- **Letter conflicts.** Local, network (including persistent ones not yet reconnected) and other `subst` letters are all treated as taken. If a letter gets taken between the check and the mapping, Short moves on to the next free one.
- **Unavailable targets at logon** are retried for about 30 seconds, then reported with a Retry option. The record is kept.
- **No elevation, ever.** Mappings are per logon session. A mapping created by an elevated process is invisible to Explorer, so Short refuses to run elevated.
- Uses `DefineDosDevice` with raw NT paths instead of calling `subst.exe`, so the *target* path itself may already be longer than 260 characters.

## Known limitations

- MAX_PATH relief applies only while you work **through the drive letter**. Opening the same file via its original path (Recent, Office MRU, a link) brings the limit back.
- Applications that resolve the real path themselves (`GetFinalPathNameByHandle`) may still fail.
- A USB drive that later receives the same global letter is hidden behind the Short drive for that user.
- Deleting through a `subst` drive may bypass the Recycle Bin on some Windows versions. Test this in your environment.
- Context-menu entries appear under *Show more options* on Windows 11. Putting them in the new top-level menu requires `IExplorerCommand` and a signed sparse MSIX package, which is out of scope for v1.

## Build

Requirements:
- **.NET 10 SDK**
- Visual Studio workload **Desktop development with C++**, because NativeAOT needs the MSVC linker

```
build.cmd
```

Output: `publish\Short.exe`, a single native executable with no runtime dependency.

> If Visual Studio 2022 doesn't see .NET 10, change `net10.0-windows` to `net8.0-windows` in `Short.csproj`.

## Install

Double-click `Short.exe` and confirm, or run `Short.exe install`.

| | |
|---|---|
| Program | `%LOCALAPPDATA%\Programs\Short\Short.exe` |
| Mapping record | `%LOCALAPPDATA%\Short\mappings.json` |
| Log | `%LOCALAPPDATA%\Short\short.log` |
| Folder menu | `HKCU\Software\Classes\Directory\shell\Short` |
| Drive menu | `HKCU\Software\Classes\Drive\shell\ShortUnshort` (only while Short drives exist) |
| Logon | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `Short` |
| Uninstall entry | `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Short` |

Everything is under `HKCU` and `%LOCALAPPDATA%`. Each user on a machine has their own installation.

## Command line

```
Short.exe create "<folder>"     Explorer: Short → Create drive
Short.exe unshort "R:\"         Explorer: Unshort
Short.exe restore               at logon
Short.exe install
Short.exe uninstall [/quiet]
```

## Project layout

```
src/Short/
  Program.cs          entry point, single-instance lock, elevation guard
  CommandLine.cs      raw parsing (Explorer passes "R:\" which breaks argv rules)
  MappingManager.cs   create / unshort / restore / uninstall logic
  DosDevices.cs       DefineDosDevice / QueryDosDevice wrappers
  MappingStore.cs     JSON record, atomic writes
  Registration.cs     HKCU menu, Run and Uninstall entries
  Installer.cs        per-user install / uninstall
  PathUtil.cs         Win32 ↔ NT paths, subst resolution
docs/
  Short-specifikacija.md   original specification (Serbian)
```

## Authors

- **Idea, specification and testing:** [Srećko Jovančević](https://github.com/sreckojovancevic)
- **Code, icon and documentation:** written by **Claude** (Anthropic), working from that specification in a conversation with Srećko

## License

MIT. See [LICENSE](LICENSE).
