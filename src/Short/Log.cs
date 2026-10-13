namespace Short;

/// <summary>Mali dnevnik u %LOCALAPPDATA%\Short\short.log — Short nema prozor, ovo je jedini trag.</summary>
internal static class Log
{
    private const long MaxBytes = 256 * 1024;

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            var fi = new FileInfo(AppPaths.LogFile);
            if (fi.Exists && fi.Length > MaxBytes)
            {
                File.Copy(AppPaths.LogFile, AppPaths.LogFile + ".1", overwrite: true);
                File.Delete(AppPaths.LogFile);
            }
            File.AppendAllText(AppPaths.LogFile,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} [{Environment.ProcessId}] {message}{Environment.NewLine}");
        }
        catch
        {
            // dnevnik nikad ne sme da obori operaciju
        }
    }
}
