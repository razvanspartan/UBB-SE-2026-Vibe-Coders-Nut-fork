namespace VibeCoders.Services;

/// <summary>
/// Resolves paths for the SQLite database files used by the application.
/// </summary>
public static class DatabasePaths
{
    /// <summary>
    /// Returns the full path to the analytics SQLite database, creating
    /// the parent directory if it does not exist.
    /// </summary>
    public static string GetAnalyticsDatabasePath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VibeCoders");
        Directory.CreateDirectory(root);
        return Path.Combine(root, "analytics.db");
    }
}
