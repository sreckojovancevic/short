using System.Runtime.InteropServices;

namespace Short;

/// <summary>Win32 pozivi. LibraryImport (source-generated) da bi radilo pod NativeAOT.</summary>
internal static unsafe partial class Native
{
    // DefineDosDevice flagovi
    public const uint DDD_RAW_TARGET_PATH = 0x1;
    public const uint DDD_REMOVE_DEFINITION = 0x2;
    public const uint DDD_EXACT_MATCH_ON_REMOVE = 0x4;

    public const int ERROR_FILE_NOT_FOUND = 2;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;

    [LibraryImport("kernel32.dll", EntryPoint = "DefineDosDeviceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DefineDosDevice(uint flags, string deviceName, string? targetPath);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial uint QueryDosDevice(string? deviceName, char* targetPath, uint ucchMax);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetLogicalDrives();

    // MessageBox
    public const uint MB_OK = 0x0;
    public const uint MB_YESNO = 0x4;
    public const uint MB_RETRYCANCEL = 0x5;
    public const uint MB_ICONERROR = 0x10;
    public const uint MB_ICONQUESTION = 0x20;
    public const uint MB_ICONWARNING = 0x30;
    public const uint MB_ICONINFORMATION = 0x40;
    public const uint MB_SETFOREGROUND = 0x10000;
    public const uint MB_TOPMOST = 0x40000;
    public const int IDRETRY = 4;
    public const int IDYES = 6;

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBox(nint hWnd, string text, string caption, uint type);

    // Shell obaveštenja (osvežavanje This PC i menija)
    public const int SHCNE_DRIVEREMOVED = 0x00000080;
    public const int SHCNE_DRIVEADD = 0x00000100;
    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0x0000;
    public const uint SHCNF_PATHW = 0x0005;
    public const uint SHCNF_FLUSH = 0x1000;

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void SHChangeNotify(int wEventId, uint uFlags, string? dwItem1, nint dwItem2);

    // Provera elevacije
    public const uint TOKEN_QUERY = 0x0008;
    public const int TokenElevationType = 18;
    public const int TokenElevationTypeFull = 2;

    [LibraryImport("kernel32.dll")]
    public static partial nint GetCurrentProcess();

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetTokenInformation(nint tokenHandle, int tokenInformationClass, void* tokenInformation, uint tokenInformationLength, out uint returnLength);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    /// <summary>
    /// True ako proces radi sa eleviranim delom podeljenog UAC tokena.
    /// Tada je u drugoj logon sesiji od Explorera i njegova mapiranja Explorer ne vidi.
    /// (UAC isključen / standardni korisnik vraćaju TokenElevationTypeDefault i to je u redu.)
    /// </summary>
    public static bool IsSplitTokenElevated()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, out var token))
            return false;
        try
        {
            int type = 0;
            return GetTokenInformation(token, TokenElevationType, &type, sizeof(int), out _)
                   && type == TokenElevationTypeFull;
        }
        finally
        {
            CloseHandle(token);
        }
    }
}
