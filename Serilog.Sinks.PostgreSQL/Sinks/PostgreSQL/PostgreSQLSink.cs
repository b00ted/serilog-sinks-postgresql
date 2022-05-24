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

        private readonly string _fullTableName;
        private readonly IDictionary<string, ColumnWriterBase> _columnOptions;
        private readonly IFormatProvider _formatProvider;
        private readonly bool _useCopy;

        private const string split ="\"";
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

            if (respectCase)
            {
                schemaName = QuoteIdentifier(schemaName);
                tableName = QuoteIdentifier(tableName);
            }
            _fullTableName = GetFullTableName(tableName, schemaName);

            _formatProvider = formatProvider;
            _useCopy = useCopy;

            _columnOptions = columnOptions ?? ColumnOptions.Default;
            if (respectCase)
            {
                _columnOptions = CreateQuotedColumnsDict(_columnOptions);
            }


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

            if (respectCase)
            {
                schemaName = QuoteIdentifier(schemaName);
                tableName = QuoteIdentifier(tableName);
            }
            _fullTableName = GetFullTableName(tableName, schemaName);

            _formatProvider = formatProvider;
            _useCopy = useCopy;

            _columnOptions = columnOptions ?? ColumnOptions.Default;
            if (respectCase)
            {
                _columnOptions = CreateQuotedColumnsDict(_columnOptions);
            }


            _isTableCreated = !needAutoCreateTable;
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (String.IsNullOrEmpty(identifier) || identifier.StartsWith("\""))
            {
                return identifier;
            }

            return $"\"{identifier}\"";
        }

        private IDictionary<string, ColumnWriterBase> CreateQuotedColumnsDict(IDictionary<string, ColumnWriterBase> originalColumnsDict)
        {
            var result = new Dictionary<string, ColumnWriterBase>(originalColumnsDict.Count);

            foreach (var kvp in originalColumnsDict)
            {
                result[QuoteIdentifier(kvp.Key)] = kvp.Value;
            }

            return result;
        }

        private string GetFullTableName(string tableName, string schemaName)
        {
            var schemaPrefix = String.Empty;
            if (!String.IsNullOrEmpty(schemaName))
            {
                schemaPrefix = split + schemaName+ split+".";
            }

            return schemaPrefix + split + tableName + split;
        }


        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                if (!_isTableCreated)
                {
                    TableCreator.CreateTable(connection, _fullTableName, _columnOptions);
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
            var columns = split+String.Join($"{split},{split}", _columnOptions.Keys)+ split;

            return $"COPY {_fullTableName}({columns}) FROM STDIN BINARY;";

        }

        private string GetInsertQuery()
        {
            var columns = $"{split}{String.Join($"{split}, {split}", _columnOptions.Keys)}{split}";

            var parameters = String.Join(", ", _columnOptions.Keys.Select(cn => ":" + ClearColumnNameForParameterName(cn)));

            return $@"INSERT INTO {_fullTableName} ({columns})
                                        VALUES ({parameters})";
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