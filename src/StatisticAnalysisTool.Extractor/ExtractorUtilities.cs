using System.Text;
using System.Text.Json;

namespace StatisticAnalysisTool.Extractor;

internal static class ExtractorUtilities
{
    private static readonly JsonWriterOptions JsonWriterOptions = new ()
    {
        Indented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    public static void WriteItem(Stream stream, IdContainer idContainer, bool first = false)
    {
        if (!first)
        {
            stream.WriteByte((byte) ',');
            stream.WriteByte((byte) '\n');
        }

        using var writer = new Utf8JsonWriter(stream, JsonWriterOptions);
        if (idContainer is ItemContainer itemContainer)
        {
            JsonSerializer.Serialize(writer, itemContainer);
        }
        else
        {
            JsonSerializer.Serialize(writer, idContainer);
        }
    }

    public static void WriteString(Stream stream, string val)
    {
        var buffer = Encoding.UTF8.GetBytes(val);
        stream.Write(buffer, 0, buffer.Length);
    }

    /// <summary>Bin names may use a Windows separator ("cluster\\world"); make them valid on the current OS.</summary>
    public static string NormalizeBinFileName(string binFileName)
    {
        return binFileName.Replace('\\', Path.DirectorySeparatorChar);
    }

    public static string GetBinFilePath(string mainGameFolder)
    {
        // Separate segments instead of ".\\Albion-Online_Data\\..." so the path also resolves on Linux.
        return Path.Combine(mainGameFolder, "Albion-Online_Data", "StreamingAssets", "GameData");
    }
}