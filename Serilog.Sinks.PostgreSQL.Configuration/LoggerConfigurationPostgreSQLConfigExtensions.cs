﻿using System;
using Microsoft.Extensions.Configuration;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace Serilog
{
    public static class LoggerConfigurationPostgreSQLConfigExtensions
    {
        /// <summary>
        /// Default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Adds a sink which writes to PostgreSQL table
        /// </summary>
        /// <param name="sinkConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The connection string to the database where to store the events.</param>
        /// <param name="tableName">Name of the table to store the events in.</param>
        /// <param name="columnOptions">Table columns writers</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSizeLimit">The maximum number of events to include to single batch.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="useCopy">If true inserts data via COPY command, otherwise uses INSERT INTO satement </param>
        /// <param name="schemaName">Schema name</param>
        /// <param name="needAutoCreateTable">Set if sink should create table</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        public static LoggerConfiguration PostgreSQL(this LoggerSinkConfiguration sinkConfiguration,
            string connectionString,
            string tableName,
            IConfiguration configuration,
            string configurationPath = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            int batchSizeLimit = PostgreSQLSink.DefaultBatchSizeLimit,
            LoggingLevelSwitch levelSwitch = null,
            bool useCopy = true,
            string schemaName = "",
            bool needAutoCreateTable = false)
        {
            if (sinkConfiguration == null)
            {
                throw new ArgumentNullException(nameof(sinkConfiguration));
            }

            var realConnectionString = connectionString;
            if (!connectionString.Contains("="))
            {
                realConnectionString = configuration.GetConnectionString(connectionString);
            }

            var columnOptions = new ConfigurationReader(configuration).GetColumnOptions(configurationPath);

            period = period ?? DefaultPeriod;

            return sinkConfiguration.Sink(new PostgreSQLSink(realConnectionString,
                                                                tableName,
                                                                period.Value,
                                                                formatProvider,
                                                                columnOptions,
                                                                batchSizeLimit,
                                                                useCopy,
                                                                schemaName,
                                                                needAutoCreateTable), restrictedToMinimumLevel, levelSwitch);
        }
    }
}
