﻿using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.PostgreSQL
{
    public class PostgreSQLSink : PeriodicBatchingSink
    {
        private readonly string _connectionString;

        private readonly string _tableName;
        private readonly IDictionary<string, ColumnWriterBase> _columnOptions;
        private readonly IFormatProvider _formatProvider;
        private readonly bool _useCopy;
        private readonly bool _respectCase;

        private readonly string _schemaName;

        public const int DefaultBatchSizeLimit = 30;
        public const int DefaultQueueLimit = Int32.MaxValue;

        private bool _isTableCreated;


        public PostgreSQLSink(string connectionString,
            string tableName,
            TimeSpan period,
            IFormatProvider formatProvider = null,
            IDictionary<string, ColumnWriterBase> columnOptions = null,
            int batchSizeLimit = DefaultBatchSizeLimit,
            bool useCopy = true,
            string schemaName = "",
            bool needAutoCreateTable = false,
            bool respectCase = false) : base(batchSizeLimit, period)
        {
            _connectionString = connectionString;
            _tableName = tableName;

            _schemaName = schemaName;

            _formatProvider = formatProvider;
            _useCopy = useCopy;
            _respectCase = respectCase;

            _columnOptions = columnOptions ?? ColumnOptions.Default;

            _isTableCreated = !needAutoCreateTable;
        }


        public PostgreSQLSink(string connectionString,
            string tableName,
            TimeSpan period,
            IFormatProvider formatProvider = null,
            IDictionary<string, ColumnWriterBase> columnOptions = null,
            int batchSizeLimit = DefaultBatchSizeLimit,
            int queueLimit = DefaultQueueLimit,
            bool useCopy = true,
            string schemaName = "",
            bool needAutoCreateTable = false,
            bool respectCase = false) : base(batchSizeLimit, period, queueLimit)
        {
            _connectionString = connectionString;
            _tableName = tableName;

            _schemaName = schemaName;

            _formatProvider = formatProvider;
            _useCopy = useCopy;
            _respectCase = respectCase;

            _columnOptions = columnOptions ?? ColumnOptions.Default;

            _isTableCreated = !needAutoCreateTable;
        }


        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                if (!_isTableCreated)
                {
                    TableCreator.CreateTable(connection, _tableName, _columnOptions, _respectCase);
                    _isTableCreated = true;
                }

                if (_useCopy)
                {
                    ProcessEventsByCopyCommand(events, connection);
                }
                else
                {
                    ProcessEventsByInsertStatements(events, connection);
                }
            }
        }

        private void ProcessEventsByInsertStatements(IEnumerable<LogEvent> events, NpgsqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetInsertQuery();


                foreach (var logEvent in events)
                {
                    //TODO: Init once
                    command.Parameters.Clear();
                    foreach (var columnOption in _columnOptions)
                    {
                        command.Parameters.AddWithValue(ClearColumnNameForParameterName(columnOption.Key), columnOption.Value.DbType,
                            columnOption.Value.GetValue(logEvent, _formatProvider));
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        private static string ClearColumnNameForParameterName(string columnName)
        {
            return columnName?.Replace("\"", "");
        }

        private void ProcessEventsByCopyCommand(IEnumerable<LogEvent> events, NpgsqlConnection connection)
        {
            using (var binaryCopyWriter = connection.BeginBinaryImport(GetCopyCommand()))
            {
                WriteToStream(binaryCopyWriter, events);
                binaryCopyWriter.Complete();
            }
        }

        private string GetCopyCommand()
        {
            string schemaPrefix = GetSchemaPrefix();
            
            if (_respectCase)
            {
                var columns = String.Join(", ", _columnOptions.Keys.Select(k => $"\"{k}\""));

                return $"COPY {schemaPrefix}\"{_tableName}\"({columns}) FROM STDIN BINARY;";
            }
            else
            {
                var columns = String.Join(", ", _columnOptions.Keys);

                return $"COPY {schemaPrefix}{_tableName}({columns}) FROM STDIN BINARY;";
            }
        }

        private string GetInsertQuery()
        {
            string schemaPrefix = GetSchemaPrefix();

            var parameters = String.Join(", ", _columnOptions.Keys.Select(cn => ":" + ClearColumnNameForParameterName(cn)));
            
            if (_respectCase)
            {
                var columns = String.Join(", ", _columnOptions.Keys.Select(k => $"\"{k}\""));

                return $"INSERT INTO {schemaPrefix}\"{ _tableName}\" ({columns}) VALUES ({parameters})";
            }
            else
            {
                var columns = String.Join(", ", _columnOptions.Keys);

                return $"INSERT INTO {schemaPrefix}{_tableName} ({columns}) VALUES ({parameters})";
            }

        }

        private string GetSchemaPrefix()
        {
            if (!String.IsNullOrEmpty(_schemaName))
            {
                return _schemaName + ".";
            }

            return String.Empty;
        }


        private void WriteToStream(NpgsqlBinaryImporter writer, IEnumerable<LogEvent> entities)
        {
            foreach (var entity in entities)
            {
                writer.StartRow();

                foreach (var columnKey in _columnOptions.Keys)
                {
                    writer.Write(_columnOptions[columnKey].GetValue(entity, _formatProvider), _columnOptions[columnKey].DbType);
                }
            }
        }
    }
}