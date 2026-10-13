namespace Short;

/// <summary>
/// Ručno parsiranje komandne linije.
/// Razlog: Explorer za koren diska prosleđuje "R:\" — standardna pravila (CommandLineToArgvW)
/// tumače \" kao escape-ovan navodnik i argument postaje R:" . Zato se ostatak linije
/// posle komande uzima doslovno i samo se skinu spoljni navodnici.
/// </summary>
internal static class CommandLine
{
    public static (string Verb, string Payload) Parse(string cmdLine)
    {
        int i = 0;
        SkipToken(cmdLine, ref i);      // putanja do exe-a
        SkipSpaces(cmdLine, ref i);

        int verbStart = i;
        while (i < cmdLine.Length && !char.IsWhiteSpace(cmdLine[i])) i++;
        string verb = cmdLine[verbStart..i].Trim('"');

        string payload = cmdLine[i..].Trim();
        if (payload.Length >= 1 && payload[0] == '"')
        {
            payload = payload[1..];
            if (payload.EndsWith('"')) payload = payload[..^1];
        }
        return (verb, payload);
    }

    private static void SkipToken(string s, ref int i)
    {
        SkipSpaces(s, ref i);
        if (i < s.Length && s[i] == '"')
        {
            i++;
            while (i < s.Length && s[i] != '"') i++;
            if (i < s.Length) i++;
        }
        else
        {
            while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;
        }
    }

    private static void SkipSpaces(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }
}
