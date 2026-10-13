<p align="center"><img src="assets/short.png" width="96" alt="Short icon"></p>

<h1 align="center">Short</h1>

<p align="center">Turn a deep folder into a drive letter with one right-click. Per-user, no admin rights.</p>

<p align="center">
  <img alt="Windows 11" src="https://img.shields.io/badge/tested%20on-Windows%2011-0078D4">
  <img alt=".NET 10 NativeAOT" src="https://img.shields.io/badge/.NET%2010-NativeAOT-512BD4">
  <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-green">
</p>

---

## Why

Windows Explorer and most of Office still hit the classic **260-character path limit (MAX_PATH)**. That holds even with `LongPathsEnabled` turned on, because Explorer is not long-path aware. Deep project trees and synced SharePoint/OneDrive libraries regularly cross that limit. Files then fail to open, copy, rename or delete, and the error messages don't say why.

The old fix is `subst R: C:\very\long\path`. The 260-character budget then starts again from `R:\`. **Short** turns that into a right-click that any user can do themselves, and it remembers the mapping across logons.

The real-world reason it exists: users who keep working in deeply nested synced SharePoint folders, and who won't switch to working online in Office, kept hitting MAX_PATH. Typing `subst` commands is not something they will do. A right-click is.

## Download

Get `Short.exe` from [Releases](../../releases), double-click it and confirm. That's the whole installation.

> The executable is not code-signed. On first run SmartScreen may show *"Windows protected your PC"* → **More info** → **Run anyway**.

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

## Tested

Tested on **Windows 11** by the author:

- [x] Build with `build.cmd` (.NET 10, NativeAOT) produces a single `Short.exe`
- [x] Per-user install by double-click, without admin rights
- [x] **Short → Create drive** on a deep folder creates the drive, which appears in *This PC* and opens the right folder
- [x] Working with files through the Short drive
- [x] Uninstall from *Add/Remove Programs* while a Short drive was active: the mapping was removed, along with the menu entries, the logon entry, the record and the program files

Not tested yet. If you check any of these, please open an issue or a PR:

- [ ] A foreign `subst` drive is left untouched by Unshort and by uninstall
- [ ] Mappings are restored after sign-out/sign-in and after a reboot
- [ ] Recycle Bin behaviour when deleting through a Short drive
- [ ] Office AutoSave and OneDrive *Files On-Demand* through a Short drive
- [ ] Windows 10

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

Short was made by a person and an AI working together.

**[Srećko Jovančević](https://github.com/sreckojovancevic): idea, specification, decisions, testing**
- Spotted the problem with his users and came up with the idea of solving MAX_PATH with `subst` behind a right-click
- Wrote the original specification ([`docs/Short-specifikacija.md`](docs/Short-specifikacija.md)), including the core rule: *never remove a mapping Short cannot reliably identify as its own*
- Made the design decisions: per-user without admin rights, no GUI, everything through Explorer, a new independent letter when shorting a folder through an existing Short drive
- Built and tested it on Windows 11, and published it

**Claude (Anthropic): review, code, icon, documentation**
- Reviewed the specification and pointed out the technical pitfalls: UAC split tokens and per-session DosDevices, why the uninstaller must run unelevated, `DefineDosDevice` shadowing instead of failing, persistent network letters, Explorer's `"R:\"` argument quoting, the Recycle Bin risk
- Wrote all of the code in `src/Short`, the build script and the solution
- Designed the icon (an original drawing, `assets/short.ico`)
- Wrote this README and the additions to the specification

## Acknowledgments

- **Claude (Anthropic)**: Short's code, icon and documentation were written by Claude, an AI model by Anthropic, working with Srećko in one conversation. Claude reviewed the specification, pointed out the technical pitfalls (UAC split tokens and per-session DosDevices, `DefineDosDevice` shadowing, Explorer's `"R:\"` quoting, the Recycle Bin risk), and wrote the implementation. The idea, the specification, the decisions and the testing on Windows 11 are Srećko's.
- **The old `subst` command**: Short is a safe, one-click wrapper around an idea that has been in Windows since DOS.
- **.NET NativeAOT**: it makes Short a single small executable with no runtime dependency.
- **CIRLAT**, the sister project ([sreckojovancevic/cirlate](https://github.com/sreckojovancevic/cirlate)): bulk-renames Cyrillic/Latin file names to ASCII. It's another way to deal with problem file names on Windows.

## License

MIT. See [LICENSE](LICENSE).
