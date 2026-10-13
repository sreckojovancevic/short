namespace Short;

internal static class AppPaths
{
    public const string AppName = "Short";

    private static string LocalAppData =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>Evidencija i dnevnik: %LOCALAPPDATA%\Short</summary>
    public static string DataDir => Path.Combine(LocalAppData, AppName);
    public static string StoreFile => Path.Combine(DataDir, "mappings.json");
    public static string LogFile => Path.Combine(DataDir, "short.log");

    /// <summary>Per-user instalacija: %LOCALAPPDATA%\Programs\Short</summary>
    public static string InstallDir => Path.Combine(LocalAppData, "Programs", AppName);
    public static string InstalledExe => Path.Combine(InstallDir, "Short.exe");

    public static string CurrentExe => Environment.ProcessPath ?? InstalledExe;

    public static bool RunningFromInstallDir =>
        string.Equals(Path.GetFullPath(CurrentExe), Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase);
}
