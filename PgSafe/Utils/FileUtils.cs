namespace PgSafe.Utils;

public static class FileUtils
{
    public static long? GetFileSize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!File.Exists(path))
            return null;

        return new FileInfo(path).Length;
    }
}