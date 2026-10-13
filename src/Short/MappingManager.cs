using System.Diagnostics;
using Microsoft.Win32;

namespace Short;

internal enum MappingState
{
    /// <summary>Slovo pokazuje tačno na evidentiranu putanju.</summary>
    Active,
    /// <summary>Slovo je slobodno; mapiranje treba (ponovo) uspostaviti.</summary>
    Free,
    /// <summary>Slovo zauzima nešto drugo. Ne dirati.</summary>
    Conflict,
}

internal sealed class MappingManager(MappingStore store)
{
    // Redosled: R..Z, pa Q..D (što dalje od slova koja Windows dodeljuje USB/optičkim uređajima)
    private static readonly char[] Candidates =
        "RSTUVWXYZ".Concat("QPONMLKJIHGFED").ToArray();

    private static string Exe => File.Exists(AppPaths.InstalledExe) ? AppPaths.InstalledExe : AppPaths.CurrentExe;

    // ───────────────────────────── stanje ─────────────────────────────

    public static MappingState StateOf(MappingRecord rec)
    {
        var cur = DosDevices.Query(rec.DriveLetter);
        if (cur != null && PathUtil.NtEquals(cur[0], rec.NtTarget))
            return MappingState.Active;
        if (cur == null && !DosDevices.IsInLogicalDrives(rec.DriveLetter[0]) && !IsPersistentNetworkLetter(rec.DriveLetter[0]))
            return MappingState.Free;
        return MappingState.Conflict;
    }

    /// <summary>Trajni mrežni diskovi koji se reconnectuju pri prijavi (možda tek posle Short-a).</summary>
    private static bool IsPersistentNetworkLetter(char letter)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Network\" + char.ToUpperInvariant(letter));
            return k != null;
        }
        catch { return true; } // u nedoumici: zauzeto
    }

    private bool IsLetterTaken(char letter)
    {
        string dev = char.ToUpperInvariant(letter) + ":";
        return DosDevices.IsInLogicalDrives(letter)
               || DosDevices.Query(dev) != null
               || IsPersistentNetworkLetter(letter)
               || store.ByDevice(dev) != null; // rezervisano za postojeći Short zapis, i kad nije aktivan
    }

    public IEnumerable<string> ActiveDevices() =>
        store.Mappings.Where(m => StateOf(m) == MappingState.Active).Select(m => m.DriveLetter).ToList();

    private void RefreshMenu() => Registration.UpdateUnshortMenu(Exe, ActiveDevices());

    // ───────────────────────────── Create ─────────────────────────────

    public void Create(string rawPath)
    {
        string full;
        try { full = PathUtil.NormalizeWin32(rawPath); }
        catch { Ui.Error($"Neispravna putanja:\n{rawPath}"); return; }

        if (!Directory.Exists(full))
        {
            Ui.Error($"Folder ne postoji ili nije dostupan:\n{full}");
            return;
        }

        // Ako je folder izabran kroz SUBST disk, mapira se stvarna putanja — mapiranja ne smeju zavisiti jedno od drugog.
        string real = PathUtil.ResolveSubst(full);
        if (!PathUtil.Win32Equals(real, full))
            Log.Write($"Create: {full} prevedeno na stvarnu putanju {real}");

        if (PathUtil.IsDriveRoot(real))
        {
            Ui.Warn($"Short mapira foldere, ne ceo disk.\n{real}");
            return;
        }
        if (!Directory.Exists(real))
        {
            Ui.Error($"Stvarni folder nije dostupan:\n{real}");
            return;
        }

        var existing = store.ByTarget(real);
        if (existing != null)
        {
            switch (StateOf(existing))
            {
                case MappingState.Active:
                    Ui.Info($"Ovaj folder je već mapiran kao {existing.DriveLetter}\n\n{real}");
                    OpenInExplorer(existing.DriveLetter);
                    return;

                case MappingState.Free:
                    if (DosDevices.TryDefineExclusive(existing.DriveLetter, existing.NtTarget))
                    {
                        Log.Write($"Create: ponovo uspostavljen {existing.DriveLetter} → {real}");
                        NotifyDrive(existing.DriveLetter, added: true);
                        RefreshMenu();
                        OpenInExplorer(existing.DriveLetter);
                        return;
                    }
                    break; // slovo se zauzelo u međuvremenu → tretira se kao konflikt

                case MappingState.Conflict:
                    break;
            }

            if (!Ui.AskYesNo(
                    $"Ovaj folder je u evidenciji kao {existing.DriveLetter}, ali to slovo sada koristi nešto drugo.\n\n" +
                    $"Dodeliti folderu novo slovo? Postojeće mapiranje na {existing.DriveLetter} neće biti dirano."))
                return;

            store.Mappings.Remove(existing);
        }

        foreach (char c in Candidates)
        {
            if (IsLetterTaken(c)) continue;

            string dev = c + ":";
            var rec = new MappingRecord { DriveLetter = dev, TargetPath = real, CreatedAt = DateTimeOffset.Now };
            if (!DosDevices.TryDefineExclusive(dev, rec.NtTarget))
                continue; // slovo zauzeto između provere i kreiranja → sledeće

            store.Mappings.Add(rec);
            try
            {
                store.Save();
            }
            catch (Exception ex)
            {
                // bez evidencije mapiranje ne bi bilo prepoznato kao Short → vraćamo stanje
                DosDevices.RemoveExact(dev, rec.NtTarget);
                Ui.Error($"Evidencija ne može da se sačuva, mapiranje nije napravljeno.\n\n{ex.Message}");
                return;
            }

            Log.Write($"Create: {dev} → {real}");
            NotifyDrive(dev, added: true);
            RefreshMenu();
            OpenInExplorer(dev);
            return;
        }

        if (existing != null)
            store.Save(); // stari zapis je uklonjen na zahtev korisnika
        Ui.Error("Nema slobodnog slova diska. Ništa nije promenjeno.");
    }

    // ───────────────────────────── Unshort ─────────────────────────────

    public void Unshort(string rawDrive)
    {
        string? dev = PathUtil.ToDevice(rawDrive);
        if (dev == null)
        {
            Ui.Error($"Nije prepoznato slovo diska: {rawDrive}");
            return;
        }

        var rec = store.ByDevice(dev);
        if (rec == null)
        {
            Ui.Warn($"{dev} nije disk koji je napravio Short. Ništa nije promenjeno.");
            RefreshMenu();
            return;
        }

        var cur = DosDevices.Query(dev);
        if (cur == null)
        {
            if (Ui.AskYesNo($"{dev} trenutno nije aktivan.\n{rec.TargetPath}\n\nUkloniti ga iz Short evidencije? Folder se ne menja."))
                RemoveRecord(rec);
            return;
        }

        if (!PathUtil.NtEquals(cur[0], rec.NtTarget))
        {
            string now = PathUtil.FromNt(cur[0]) ?? cur[0];
            if (Ui.AskYesNo(
                    $"{dev} više ne pokazuje na folder iz Short evidencije.\n\n" +
                    $"Evidencija: {rec.TargetPath}\nSada: {now}\n\n" +
                    "Mapiranje neće biti dirano. Ukloniti samo zapis iz Short evidencije?"))
                RemoveRecord(rec);
            return;
        }

        if (!DosDevices.RemoveExact(dev, rec.NtTarget) || IsStillOurs(rec))
        {
            Ui.Error($"Uklanjanje {dev} nije uspelo. Ništa nije promenjeno.");
            return;
        }

        Log.Write($"Unshort: {dev} → {rec.TargetPath}");
        NotifyDrive(dev, added: false);
        RemoveRecord(rec);
    }

    private static bool IsStillOurs(MappingRecord rec)
    {
        var cur = DosDevices.Query(rec.DriveLetter);
        return cur != null && PathUtil.NtEquals(cur[0], rec.NtTarget);
    }

    private void RemoveRecord(MappingRecord rec)
    {
        store.Mappings.Remove(rec);
        store.Save();
        RefreshMenu();
    }

    // ───────────────────────────── Restore (logon) ─────────────────────────────

    public void Restore(int waitAttempts = 6, int waitSeconds = 5)
    {
        while (true)
        {
            var problems = RestorePass(waitAttempts, waitSeconds);
            RefreshMenu();
            if (problems.Count == 0) return;

            string text = "Neka Short mapiranja nisu obnovljena:\n\n" + string.Join("\n", problems) +
                          "\n\nPostojeća mapiranja nisu menjana. Zapisi su sačuvani.";
            if (!Ui.AskRetry(text)) return;
            waitAttempts = 1;
        }
    }

    private List<string> RestorePass(int attempts, int waitSeconds)
    {
        var pending = store.Mappings.ToList();
        var conflicts = new List<string>();

        for (int attempt = 1; ; attempt++)
        {
            var unavailable = new List<MappingRecord>();
            foreach (var rec in pending)
            {
                switch (StateOf(rec))
                {
                    case MappingState.Active:
                        break;
                    case MappingState.Conflict:
                        conflicts.Add($"{rec.DriveLetter}  slovo je zauzeto  ({rec.TargetPath})");
                        break;
                    case MappingState.Free:
                        if (!Directory.Exists(rec.TargetPath))
                            unavailable.Add(rec);
                        else if (DosDevices.TryDefineExclusive(rec.DriveLetter, rec.NtTarget))
                        {
                            Log.Write($"Restore: {rec.DriveLetter} → {rec.TargetPath}");
                            NotifyDrive(rec.DriveLetter, added: true);
                        }
                        else
                            conflicts.Add($"{rec.DriveLetter}  slovo je zauzeto  ({rec.TargetPath})");
                        break;
                }
            }

            if (unavailable.Count == 0 || attempt >= attempts)
            {
                conflicts.AddRange(unavailable.Select(r => $"{r.DriveLetter}  folder nije dostupan  ({r.TargetPath})"));
                return conflicts;
            }

            // pri prijavi mreža / sync možda još nisu spremni
            Thread.Sleep(TimeSpan.FromSeconds(waitSeconds));
            pending = unavailable;
        }
    }

    // ───────────────────────────── Deinstalacija ─────────────────────────────

    /// <summary>Uklanja samo potvrđena Short mapiranja. Vraća listu onoga što je ostalo.</summary>
    public List<string> RemoveAllConfirmed()
    {
        var leftovers = new List<string>();
        foreach (var rec in store.Mappings.ToList())
        {
            var cur = DosDevices.Query(rec.DriveLetter);
            if (cur == null || !cur.Any(c => PathUtil.NtEquals(c, rec.NtTarget)))
            {
                Log.Write($"Uninstall: {rec.DriveLetter} nije aktivan kao Short mapiranje, preskačem");
                if (cur != null)
                    leftovers.Add($"{rec.DriveLetter}  pokazuje na nešto drugo, nije dirano");
                continue;
            }

            if (!PathUtil.NtEquals(cur[0], rec.NtTarget))
            {
                // naše mapiranje postoji, ali je zasenčeno tuđim — uklanjamo samo naše (exact match)
                Log.Write($"Uninstall: {rec.DriveLetter} je zasenčen, uklanjam samo Short definiciju");
            }

            if (DosDevices.RemoveExact(rec.DriveLetter, rec.NtTarget) &&
                DosDevices.Query(rec.DriveLetter)?.Any(c => PathUtil.NtEquals(c, rec.NtTarget)) != true)
            {
                Log.Write($"Uninstall: uklonjen {rec.DriveLetter} → {rec.TargetPath}");
                NotifyDrive(rec.DriveLetter, added: false);
            }
            else
            {
                leftovers.Add($"{rec.DriveLetter}  uklanjanje nije uspelo ({rec.TargetPath}); nestaće pri odjavi");
            }
        }
        return leftovers;
    }

    // ───────────────────────────── pomoćno ─────────────────────────────

    private static void NotifyDrive(string dev, bool added) =>
        Native.SHChangeNotify(added ? Native.SHCNE_DRIVEADD : Native.SHCNE_DRIVEREMOVED,
            Native.SHCNF_PATHW | Native.SHCNF_FLUSH, dev + "\\", 0);

    private static void OpenInExplorer(string dev)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", dev + "\\") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Write($"Otvaranje {dev} u Exploreru: {ex.Message}");
        }
    }
}
