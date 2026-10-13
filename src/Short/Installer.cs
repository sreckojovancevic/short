using System.Diagnostics;

namespace Short;

internal static class Installer
{
    public static void Install()
    {
        string src = AppPaths.CurrentExe;
        string dst = AppPaths.InstalledExe;

        if (!AppPaths.RunningFromInstallDir)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.InstallDir);
                File.Copy(src, dst, overwrite: true);
            }
            catch (Exception ex)
            {
                Ui.Error($"Kopiranje u {AppPaths.InstallDir} nije uspelo.\n\n{ex.Message}\n\n" +
                         "Ako je Short već instaliran, sačekaj par sekundi i pokušaj ponovo.");
                return;
            }
        }

        Registration.RegisterAll(dst);

        // nadogradnja: postojeća evidencija ostaje, mapiranja se obnavljaju
        var store = MappingStore.Load();
        new MappingManager(store).Restore();
        Registration.NotifyShellAssociationsChanged();

        Log.Write($"Instalirano u {AppPaths.InstallDir}");
        Ui.Info("Short je instaliran.\n\n" +
                "Desni klik na folder → Short → Create drive\n" +
                "Desni klik na Short disk → Unshort\n\n" +
                "Na Windows 11 stavke su pod \"Show more options\".");
    }

    public static void Uninstall()
    {
        var report = new List<string>();

        // 1–3. mapiranja: samo potvrđena Short mapiranja
        MappingStore? store = null;
        try
        {
            store = MappingStore.Load();
        }
        catch (StoreCorruptException ex)
        {
            report.Add("Evidencija je oštećena, mapiranja nisu mogla da se provere i nisu dirana: " + ex.Message);
        }

        if (store != null)
            report.AddRange(new MappingManager(store).RemoveAllConfirmed());

        // 4–5. meniji i automatsko pokretanje
        report.AddRange(Registration.UnregisterAll());
        Registration.NotifyShellAssociationsChanged();

        // 5. evidencija i dnevnik (posle obrade mapiranja)
        try
        {
            if (Directory.Exists(AppPaths.DataDir))
                Directory.Delete(AppPaths.DataDir, recursive: true);
        }
        catch (Exception ex)
        {
            report.Add($"{AppPaths.DataDir} nije obrisan: {ex.Message}");
        }

        Registration.RemoveUninstallEntry();

        // fajlovi aplikacije: proces ne može da obriše sam sebe, briše ga cmd posle izlaska
        ScheduleInstallDirRemoval();

        if (report.Count == 0)
            Ui.Info("Short je uklonjen.\nSva Short mapiranja, stavke menija i automatsko pokretanje su uklonjeni.");
        else
            Ui.Warn("Short je uklonjen, ali čišćenje NIJE potpuno:\n\n" + string.Join("\n", report) +
                    "\n\nCiljni folderi i njihov sadržaj nisu dirani.");
    }

    private static void ScheduleInstallDirRemoval()
    {
        if (!Directory.Exists(AppPaths.InstallDir)) return;
        try
        {
            string dir = AppPaths.InstallDir;
            var psi = new ProcessStartInfo("cmd.exe",
                $"/d /c ping 127.0.0.1 -n 4 >nul & rmdir /s /q \"{dir}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath(),
            };
            Process.Start(psi);
        }
        catch
        {
            // ako ne uspe, ostaje samo folder sa exe-om; nema menija ni pokretanja
        }
    }
}
