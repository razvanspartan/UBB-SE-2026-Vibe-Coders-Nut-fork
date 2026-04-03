namespace VibeCoders.Services;

public static class DatabasePaths
{
    public static string GetConnectionString()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VibeCoders");

        Directory.CreateDirectory(folder);

        string dbPath = Path.Combine(folder, "vibecoders.db");
        return $"Data Source={dbPath}";
    }
}
