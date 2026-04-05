using System.Reflection;
using Microsoft.Data.Sqlite;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage : IDataStorage
    {
        private readonly string _connectionString = DatabasePaths.GetConnectionString();

        public void EnsureSchemaCreated()
        {
            string sql = LoadSchemaSql();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static string LoadSchemaSql()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly
                .GetManifestResourceNames()
                .First(n => n.EndsWith("schema.sql", StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}