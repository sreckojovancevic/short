namespace Short;

internal static class Ui
{
    private const string Title = "Short";
    private const uint Base = Native.MB_SETFOREGROUND | Native.MB_TOPMOST;

    /// <summary>U tihom režimu (uninstall /quiet) poruke idu samo u dnevnik.</summary>
    public static bool Quiet { get; set; }

    public static void Info(string text) => Show(text, Native.MB_OK | Native.MB_ICONINFORMATION);
    public static void Warn(string text) => Show(text, Native.MB_OK | Native.MB_ICONWARNING);
    public static void Error(string text) => Show(text, Native.MB_OK | Native.MB_ICONERROR);

    public static bool AskYesNo(string text, bool quietDefault = false)
    {
        if (Quiet) { Log.Write("[pitanje, tihi režim → " + (quietDefault ? "da" : "ne") + "] " + text); return quietDefault; }
        return Native.MessageBox(0, text, Title, Base | Native.MB_YESNO | Native.MB_ICONQUESTION) == Native.IDYES;
    }

    public static bool AskRetry(string text)
    {
        if (Quiet) { Log.Write("[poruka] " + text); return false; }
        return Native.MessageBox(0, text, Title, Base | Native.MB_RETRYCANCEL | Native.MB_ICONWARNING) == Native.IDRETRY;
    }

    private static void Show(string text, uint flags)
    {
        Log.Write("[poruka] " + text.Replace(Environment.NewLine, " / "));
        if (!Quiet)
            Native.MessageBox(0, text, Title, Base | flags);
    }
}
