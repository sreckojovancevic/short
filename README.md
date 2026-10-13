# Short

Folder → slovo diska (SUBST) preko desnog klika u Exploreru. Per-user, bez admin prava.
Specifikacija: [`docs/Short-specifikacija.md`](docs/Short-specifikacija.md).

## Build

Potrebno:
- **.NET 10 SDK** (dolazi sa Visual Studio 2026; za VS 2022 vidi napomenu ispod)
- Visual Studio Installer → workload **Desktop development with C++** (NativeAOT koristi MSVC linker)

```
build.cmd
```

Rezultat: `publish\Short.exe` (jedan fajl, bez runtime-a).

Iz Visual Studio-a: otvori `Short.sln`, desni klik na projekat → **Publish** → Folder, ili samo pokreni `build.cmd`.
Običan **Build** (F5) pravi JIT verziju u `bin\` — radi isto, samo nije AOT.

> Ako VS 2022 ne vidi .NET 10: u `Short.csproj` promeni `net10.0-windows` u `net8.0-windows`. Kod je kompatibilan.

## Instalacija

Dupli klik na `Short.exe` → „Instalirati Short?“ → Yes. Ili `Short.exe install`.

- kopira se u `%LOCALAPPDATA%\Programs\Short\Short.exe`
- meni, automatsko pokretanje i unos u Apps & Features idu u `HKCU`
- admin prava nisu potrebna; **ne pokretati kao administrator** (Short tada odbija rad)

## Komande

| Komanda | Ko je poziva |
|---|---|
| `Short.exe create "<folder>"` | desni klik na folder → Short → Create drive |
| `Short.exe unshort "R:\"` | desni klik na Short disk → Unshort |
| `Short.exe restore` | pri prijavi (`HKCU\...\Run`) |
| `Short.exe install` | ručno / dupli klik |
| `Short.exe uninstall [/quiet]` | Settings → Apps → Short |

## Gde je šta

| | |
|---|---|
| Evidencija | `%LOCALAPPDATA%\Short\mappings.json` |
| Dnevnik | `%LOCALAPPDATA%\Short\short.log` |
| Meni foldera | `HKCU\Software\Classes\Directory\shell\Short` |
| Meni diska | `HKCU\Software\Classes\Drive\shell\ShortUnshort` (postoji samo dok ima aktivnih Short diskova) |
| Pokretanje | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `Short` |
| Deinstalacija | `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Short` |

## Šta testirati prvo

Short nije testiran na Windowsu — kod je proveren samo kompajlerom. Redosled:

1. **Create**: desni klik na duboki folder → Show more options → Short → Create drive. Očekivano: otvara se `R:\`, disk je u This PC.
2. **Unshort se vidi samo na R:** — desni klik na `C:` ne sme da nudi Unshort. Ako se Unshort *ne pojavi ni na R:*, problem je `AppliesTo` uslov (`Registration.UpdateUnshortMenu`); javi i menjamo uslov.
3. **Ponovni Create** na istom folderu → poruka „već mapiran kao R:“.
4. **Create kroz R:** (`R:\podfolder`) → novo slovo; `subst` u cmd treba da pokaže stvarnu putanju, ne `R:\...`.
5. **Unshort** → R: nestaje, folder netaknut.
6. **Odjava/prijava i restart** → mapiranja se vraćaju.
7. **Tuđi subst**: `subst T: C:\Windows`, pa `Short.exe unshort T:` → poruka, T: ostaje.
8. **Recycle Bin**: obriši fajl preko `R:` — ide li u korpu?
9. **Office + OneDrive/SharePoint sync folder** preko `R:` — otvaranje, AutoSave, Files On-Demand.
10. **Deinstalacija** iz Settings → Apps → nema R:, nema menija, nema Run vrednosti, nema `%LOCALAPPDATA%\Short`.

Ako nešto ne radi: `%LOCALAPPDATA%\Short\short.log`.
