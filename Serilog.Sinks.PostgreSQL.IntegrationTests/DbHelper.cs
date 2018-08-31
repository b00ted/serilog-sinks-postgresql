using Npgsql;

namespace Serilog.Sinks.PostgreSQL.IntegrationTests
{
    public class DbHelper
    {
        private string _connectionString;

        public DbHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void RemoveTable(string tableName, bool respectCase = false)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = respectCase
                        ? $"DROP TABLE IF EXISTS \"{tableName}\""
                        : $"DROP TABLE IF EXISTS {tableName}";

                    command.ExecuteNonQuery();
                }
            }
        }

        public void ClearTable(string tableName)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"TRUNCATE {tableName}";

                    command.ExecuteNonQuery();
                }
            }
        }

        public long GetTableRowsCount(string tableName, bool respectCase = false)
        {
            var sql = respectCase
                ? $"SELECT count(*) FROM \"{tableName}\""
                : $"SELECT count(*) FROM {tableName}";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;

                    return (long) command.ExecuteScalar();
                }
            }
        }
    }
}