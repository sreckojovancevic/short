using Microsoft.Win32;

namespace Short;

/// <summary>
/// Sve što Short upisuje u registry. Isključivo HKCU — nema admin prava, nema HKLM.
/// </summary>
internal static class Registration
{
    // Kontekstni meni foldera: Short → Create drive
    private const string FolderMenuKey = @"Software\Classes\Directory\shell\Short";
    // Kontekstni meni diska: Unshort (samo na Short diskovima, preko AppliesTo)
    private const string DriveMenuKey = @"Software\Classes\Drive\shell\ShortUnshort";
    // Pokretanje pri prijavi
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "Short";
    // Apps & Features (per-user)
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Short";

    private static string Q(string s) => "\"" + s + "\"";

    public static void RegisterAll(string exe)
    {
        // Short → Create drive (kaskadni podmeni)
        using (var k = Registry.CurrentUser.CreateSubKey(FolderMenuKey))
        {
            k.SetValue("MUIVerb", "Short");
            k.SetValue("Icon", exe);
            k.SetValue("SubCommands", "");
            using var create = k.CreateSubKey(@"shell\01create");
            create.SetValue("MUIVerb", "Create drive");
            create.SetValue("Icon", exe);
            create.SetValue("MultiSelectModel", "Single");
            using var cmd = create.CreateSubKey("command");
            cmd.SetValue("", $"{Q(exe)} create \"%1\"");
        }

        using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
            k.SetValue(RunValue, $"{Q(exe)} restore");

        using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            k.SetValue("DisplayName", "Short");
            k.SetValue("DisplayVersion", typeof(Registration).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
            k.SetValue("Publisher", "Short");
            k.SetValue("DisplayIcon", exe);
            k.SetValue("InstallLocation", Path.GetDirectoryName(exe) ?? "");
            k.SetValue("UninstallString", $"{Q(exe)} uninstall");
            k.SetValue("QuietUninstallString", $"{Q(exe)} uninstall /quiet");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            try
            {
                k.SetValue("EstimatedSize", (int)(new FileInfo(exe).Length / 1024), RegistryValueKind.DWord);
            }
            catch { /* nije bitno */ }
        }
    }

    /// <summary>
    /// Ažurira Unshort stavku tako da se pojavljuje samo na aktivnim Short diskovima.
    /// Bez aktivnih Short diskova stavka ne postoji.
    /// Unshort svejedno pri izvršenju proverava evidenciju i stvarno mapiranje;
    /// AppliesTo je samo da se stavka ne nudi na tuđim diskovima.
    /// </summary>
    public static void UpdateUnshortMenu(string exe, IEnumerable<string> activeDevices)
    {
        var devices = activeDevices.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d).ToList();
        if (devices.Count == 0)
        {
            Registry.CurrentUser.DeleteSubKeyTree(DriveMenuKey, throwOnMissingSubKey: false);
        }
        else
        {
            // AQS: System.ItemPathDisplay:="R:\" OR System.ItemPathDisplay:="S:\"
            string appliesTo = string.Join(" OR ", devices.Select(d => $"System.ItemPathDisplay:=\"{d}\\\""));
            using var k = Registry.CurrentUser.CreateSubKey(DriveMenuKey);
            k.SetValue("MUIVerb", "Unshort");
            k.SetValue("Icon", exe);
            k.SetValue("AppliesTo", appliesTo);
            k.SetValue("MultiSelectModel", "Single");
            using var cmd = k.CreateSubKey("command");
            cmd.SetValue("", $"{Q(exe)} unshort \"%1\"");
        }
        Log.Write("Unshort meni: " + (devices.Count == 0 ? "uklonjen" : string.Join(",", devices)));
    }

    /// <summary>Uklanja sve stavke. Vraća listu onoga što nije moglo da se ukloni.</summary>
    public static List<string> UnregisterAll()
    {
        var failed = new List<string>();
        void Del(string key, string label)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(key, throwOnMissingSubKey: false);
                if (Registry.CurrentUser.OpenSubKey(key) is { } still) { still.Dispose(); failed.Add(label); }
            }
            catch (Exception ex) { Log.Write($"Brisanje {key}: {ex.Message}"); failed.Add(label); }
        }

        Del(FolderMenuKey, "stavka menija \"Short → Create drive\"");
        Del(DriveMenuKey, "stavka menija \"Unshort\"");

        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            run?.DeleteValue(RunValue, throwOnMissingValue: false);
            if (run?.GetValue(RunValue) != null) failed.Add("automatsko pokretanje pri prijavi");
        }
        catch (Exception ex) { Log.Write($"Brisanje Run vrednosti: {ex.Message}"); failed.Add("automatsko pokretanje pri prijavi"); }

        return failed;
    }

    public static void RemoveUninstallEntry() =>
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, throwOnMissingSubKey: false);

    public static void NotifyShellAssociationsChanged() =>
        Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST | Native.SHCNF_FLUSH, null, 0);
}
