using System.Runtime.InteropServices;

namespace Short;

/// <summary>
/// Rad sa DosDevices prostorom trenutne logon sesije (isto što radi subst.exe, ali direktno).
/// Ciljevi se zadaju kao NT putanje sa DDD_RAW_TARGET_PATH, pa za ciljnu putanju ne važi MAX_PATH.
/// </summary>
internal static class DosDevices
{
    /// <summary>
    /// Vraća sve definicije za ime uređaja ("R:"): prva je trenutna, ostale su ranije
    /// neobrisane definicije koje su zasenčene. Null ako uređaj ne postoji.
    /// </summary>
    public static unsafe string[]? Query(string device)
    {
        int size = 1024;
        while (size <= 1 << 20)
        {
            var buffer = new char[size];
            uint written;
            fixed (char* p = buffer)
                written = Native.QueryDosDevice(device, p, (uint)size);

            if (written == 0)
            {
                int err = Marshal.GetLastPInvokeError();
                if (err == Native.ERROR_INSUFFICIENT_BUFFER) { size *= 4; continue; }
                return null; // ERROR_FILE_NOT_FOUND ili slično: nije definisan
            }

            var list = new List<string>();
            int start = 0;
            for (int i = 0; i < written; i++)
            {
                if (buffer[i] != '\0') continue;
                if (i == start) break; // dvostruka nula: kraj liste
                list.Add(new string(buffer, start, i - start));
                start = i + 1;
            }
            return list.Count == 0 ? null : list.ToArray();
        }
        return null;
    }

    /// <summary>Trenutna (aktivna) definicija ili null.</summary>
    public static string? Current(string device) => Query(device)?[0];

    /// <summary>
    /// Definiše device → ntTarget i proverava da nismo zasenčili postojeću definiciju
    /// (DefineDosDevice ne odbija zauzeto ime, već ga gura na stek).
    /// Ako je rezultat bilo šta osim "samo naša definicija", vraća stanje kakvo je bilo.
    /// </summary>
    public static bool TryDefineExclusive(string device, string ntTarget)
    {
        if (Query(device) != null)
            return false;

        if (!Native.DefineDosDevice(Native.DDD_RAW_TARGET_PATH, device, ntTarget))
        {
            Log.Write($"DefineDosDevice({device}) neuspešno, greška {Marshal.GetLastPInvokeError()}");
            return false;
        }

        var after = Query(device);
        bool ok = after is { Length: 1 } && PathUtil.NtEquals(after[0], ntTarget);
        if (!ok)
        {
            Log.Write($"{device}: posle definisanja zatečeno stanje [{string.Join(" | ", after ?? [])}], vraćam");
            RemoveExact(device, ntTarget);
        }
        return ok;
    }

    /// <summary>
    /// Uklanja isključivo definiciju device → ntTarget (DDD_EXACT_MATCH_ON_REMOVE).
    /// Ako slovo pokazuje na nešto drugo, Windows odbija uklanjanje.
    /// </summary>
    public static bool RemoveExact(string device, string ntTarget)
    {
        bool ok = Native.DefineDosDevice(
            Native.DDD_RAW_TARGET_PATH | Native.DDD_REMOVE_DEFINITION | Native.DDD_EXACT_MATCH_ON_REMOVE,
            device, ntTarget);
        if (!ok)
            Log.Write($"Uklanjanje {device} → {ntTarget} neuspešno, greška {Marshal.GetLastPInvokeError()}");
        return ok;
    }

    public static bool IsInLogicalDrives(char letter)
    {
        uint mask = Native.GetLogicalDrives();
        return (mask & (1u << (char.ToUpperInvariant(letter) - 'A'))) != 0;
    }
}
