namespace Short;

internal static class PathUtil
{
    /// <summary>"r", "R:", "R:\" → "R:"; null ako nije slovo diska.</summary>
    public static string? ToDevice(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"');
        if (s.Length == 0 || !char.IsAsciiLetter(s[0])) return null;
        if (s.Length > 1 && s[1] != ':') return null;
        if (s.Length > 2 && s.TrimEnd('\\', '"').Length > 2) return null;
        return char.ToUpperInvariant(s[0]) + ":";
    }

    /// <summary>Puna Win32 putanja bez završnog '\' (osim za koren diska).</summary>
    public static string NormalizeWin32(string path)
    {
        string full = Path.GetFullPath(path.Trim().Trim('"'));
        if (full.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            full = @"\\" + full[8..];
        else if (full.StartsWith(@"\\?\", StringComparison.Ordinal))
            full = full[4..];

        if (full.Length > 3 && full.EndsWith('\\'))
            full = full.TrimEnd('\\');
        return full;
    }

    /// <summary>Win32 putanja → NT putanja za DefineDosDevice(DDD_RAW_TARGET_PATH).</summary>
    public static string ToNt(string win32)
    {
        string p = NormalizeWin32(win32);
        if (p.StartsWith(@"\\", StringComparison.Ordinal))
            return @"\??\UNC\" + p[2..];
        return @"\??\" + p;
    }

    /// <summary>
    /// NT cilj subst-tipa → Win32 putanja. Null za prave uređaje
    /// (\Device\HarddiskVolume3, \Device\LanmanRedirector\..., itd.).
    /// </summary>
    public static string? FromNt(string? nt)
    {
        if (nt == null) return null;
        if (nt.StartsWith(@"\??\UNC\", StringComparison.OrdinalIgnoreCase))
            return @"\\" + nt[8..];
        if (nt.StartsWith(@"\??\", StringComparison.Ordinal) && nt.Length >= 6 && nt[5] == ':')
            return nt[4..];
        return null;
    }

    public static bool NtEquals(string a, string b) =>
        string.Equals(a.TrimEnd('\\'), b.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

    public static bool Win32Equals(string a, string b) =>
        string.Equals(NormalizeWin32(a), NormalizeWin32(b), StringComparison.OrdinalIgnoreCase);

    public static bool IsDriveRoot(string win32) =>
        win32.Length <= 3 && win32.Length >= 2 && win32[1] == ':';

    /// <summary>
    /// Ako putanja ide kroz SUBST disk (Short ili tuđi), prevodi je na stvarnu putanju.
    /// Tako novo mapiranje nikada ne zavisi od drugog SUBST diska.
    /// </summary>
    public static string ResolveSubst(string win32)
    {
        string p = NormalizeWin32(win32);
        for (int depth = 0; depth < 16; depth++)
        {
            string? dev = p.Length >= 2 && p[1] == ':' ? ToDevice(p[..2]) : null;
            if (dev == null) break;

            string? target = FromNt(DosDevices.Current(dev));
            if (target == null) break; // pravi disk

            string rest = p.Length > 2 ? p[2..].TrimStart('\\') : "";
            p = rest.Length == 0 ? target : target.TrimEnd('\\') + "\\" + rest;
            p = NormalizeWin32(p);
        }
        return p;
    }
}
