namespace VibeCoders.Services;

/// <summary>
/// Resolves database connection settings used by the application.
/// </summary>
public static class DatabasePaths
{
    /// <summary>
    /// SQL Server connection string for LocalDB (same database as <see cref="SqlDataStorage"/>).
    /// Analytics tables (<c>workout_log</c>, etc.) live in this database.
    /// </summary>
    public static string GetSqlServerConnectionString() =>
        @"Server=(localdb)\MSSQLLocalDB;Database=VibeCodersDB;Trusted_Connection=True;";
}
