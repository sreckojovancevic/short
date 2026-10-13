namespace Short;

/// <summary>
/// Short.exe create  "D:\neka\duboka\putanja"   — desni klik na folder
/// Short.exe unshort "R:\"                     — desni klik na Short disk
/// Short.exe restore                           — pri prijavi (HKCU Run)
/// Short.exe install                           — per-user instalacija
/// Short.exe uninstall [/quiet]                — Apps &amp; Features
/// Short.exe                                   — bez argumenata: ponudi instalaciju
/// </summary>
internal static class Program
{
    private const string MutexName = @"Local\Short.Mappings.7C1B3E4A";

    [STAThread]
    private static int Main()
    {
        var (verb, payload) = CommandLine.Parse(Environment.CommandLine);
        verb = verb.ToLowerInvariant();
        Ui.Quiet = verb == "uninstall" && payload.Contains("/quiet", StringComparison.OrdinalIgnoreCase);
        Log.Write($"start: {verb} {payload}");

        if (Native.IsSplitTokenElevated())
        {
            Ui.Error("Short je pokrenut sa administratorskim pravima.\n\n" +
                     "Mapiranja napravljena ovako Explorer ne bi video. Pokreni Short kao običan korisnik.");
            return 2;
        }

        using var mutex = new Mutex(false, MutexName);
        try
        {
            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(60)))
                {
                    Ui.Error("Druga Short operacija je u toku. Pokušaj ponovo.");
                    return 3;
                }
            }
            catch (AbandonedMutexException) { /* prethodni proces je pao; mutex je sada naš */ }

            return Run(verb, payload);
        }
        catch (StoreCorruptException ex)
        {
            Log.Write(ex.ToString());
            Ui.Error(ex.Message + "\n\nShort neće menjati mapiranja dok evidencija nije ispravna.");
            return 4;
        }
        catch (Exception ex)
        {
            Log.Write(ex.ToString());
            Ui.Error("Neočekivana greška:\n\n" + ex.Message);
            return 1;
        }
        finally
        {
            try { mutex.ReleaseMutex(); } catch { }
        }
    }

    private static int Run(string verb, string payload)
    {
        switch (verb)
        {
            case "create":
                new MappingManager(MappingStore.Load()).Create(payload);
                return 0;

            case "unshort":
                new MappingManager(MappingStore.Load()).Unshort(payload);
                return 0;

            case "restore":
                new MappingManager(MappingStore.Load()).Restore();
                return 0;

            case "install":
                Installer.Install();
                return 0;

            case "uninstall":
                Installer.Uninstall();
                return 0;

            case "":
                if (AppPaths.RunningFromInstallDir)
                    Ui.Info("Short je instaliran.\n\n" +
                            "Desni klik na folder → Short → Create drive\n" +
                            "Desni klik na Short disk → Unshort\n\n" +
                            "Deinstalacija: Settings → Apps → Short.");
                else if (Ui.AskYesNo("Instalirati Short za trenutnog korisnika?\n\nAdministratorska prava nisu potrebna."))
                    Installer.Install();
                return 0;

            default:
                Ui.Error($"Nepoznata komanda: {verb}");
                return 1;
        }
    }
}
