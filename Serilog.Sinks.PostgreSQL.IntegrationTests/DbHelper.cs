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

        public void RemoveTable(string tableName)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"DROP TABLE IF EXISTS {tableName}";

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

        public long GetTableRowsCount(string tableName)
        {
            var sql = $@"SELECT count(*)
                         FROM {tableName}";

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

        public int GetCharColumnLength(string tableName, string columnName)
        {
            var sql = $@"select COALESCE(character_maximum_length, 0)
                                from INFORMATION_SCHEMA.COLUMNS
                                where table_name = '{tableName}'
	                                and column_name = '{columnName}'";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;

                    return (int)command.ExecuteScalar();
                }
            }
        }
    }
}