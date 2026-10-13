using System.Text.Json;
using System.Text.Json.Serialization;

namespace Short;

internal sealed class MappingRecord
{
    public const int CurrentSchema = 1;

    /// <summary>"R:"</summary>
    public string DriveLetter { get; set; } = "";
    /// <summary>Apsolutna Win32 putanja foldera (stvarna, nikad preko drugog SUBST diska).</summary>
    public string TargetPath { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public int SchemaVersion { get; set; } = CurrentSchema;

    [JsonIgnore] public string NtTarget => PathUtil.ToNt(TargetPath);
}

internal sealed class StoreFile
{
    public int SchemaVersion { get; set; } = MappingRecord.CurrentSchema;
    public List<MappingRecord> Mappings { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(StoreFile))]
internal sealed partial class StoreJsonContext : JsonSerializerContext;

internal sealed class StoreCorruptException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Vlasnička evidencija Short-a. Jedini izvor informacije o tome šta je Short napravio,
/// ali se nikad ne prihvata bez provere stvarnog stanja Windowsa.
/// </summary>
internal sealed class MappingStore
{
    public List<MappingRecord> Mappings { get; private set; } = [];

    public static MappingStore Load()
    {
        var store = new MappingStore();
        if (!File.Exists(AppPaths.StoreFile))
            return store;

        StoreFile? data;
        try
        {
            data = JsonSerializer.Deserialize(File.ReadAllText(AppPaths.StoreFile), StoreJsonContext.Default.StoreFile);
        }
        catch (Exception ex)
        {
            throw new StoreCorruptException($"Evidencija {AppPaths.StoreFile} ne može da se pročita.", ex);
        }

        if (data == null)
            throw new StoreCorruptException($"Evidencija {AppPaths.StoreFile} je prazna ili neispravna.");
        if (data.SchemaVersion > MappingRecord.CurrentSchema)
            throw new StoreCorruptException($"Evidencija {AppPaths.StoreFile} je iz novije verzije Short-a (format {data.SchemaVersion}).");

        foreach (var m in data.Mappings)
        {
            if (PathUtil.ToDevice(m.DriveLetter) is not { } dev || string.IsNullOrWhiteSpace(m.TargetPath))
                throw new StoreCorruptException($"Evidencija {AppPaths.StoreFile} sadrži neispravan zapis.");
            m.DriveLetter = dev;
        }

        store.Mappings = data.Mappings;
        return store;
    }

    /// <summary>Atomičan upis: privremeni fajl + zamena. Pokvaren poluupis ne sme da postoji.</summary>
    public void Save()
    {
        Directory.CreateDirectory(AppPaths.DataDir);
        var data = new StoreFile { Mappings = Mappings };
        string json = JsonSerializer.Serialize(data, StoreJsonContext.Default.StoreFile);
        string tmp = AppPaths.StoreFile + ".tmp";

        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
        {
            sw.Write(json);
            sw.Flush();
            fs.Flush(flushToDisk: true);
        }
        File.Move(tmp, AppPaths.StoreFile, overwrite: true);
    }

    public MappingRecord? ByDevice(string device) =>
        Mappings.FirstOrDefault(m => string.Equals(m.DriveLetter, device, StringComparison.OrdinalIgnoreCase));

    public MappingRecord? ByTarget(string win32) =>
        Mappings.FirstOrDefault(m => PathUtil.Win32Equals(m.TargetPath, win32));
}
