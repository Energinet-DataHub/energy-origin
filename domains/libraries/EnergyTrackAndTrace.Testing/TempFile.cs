using System.IO;

namespace EnergyTrackAndTrace.Testing;

public static class TempFile
{
    public static string WriteAllText(string content)
    {
        string tempFileName = Path.GetTempFileName();
        File.WriteAllText(tempFileName, content);
        return tempFileName;
    }

    public static string WriteAllText(string content, string extension)
    {
        string path = Path.ChangeExtension(Path.GetTempFileName(), extension);
        File.WriteAllText(path, content);
        return path;
    }
}
