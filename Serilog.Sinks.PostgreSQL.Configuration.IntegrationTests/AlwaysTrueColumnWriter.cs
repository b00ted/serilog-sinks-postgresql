using System;
using NpgsqlTypes;
using Serilog.Events;

namespace Serilog.Sinks.PostgreSQL.Configuration.IntegrationTests
{
    public class AlwaysTrueColumnWriter: ColumnWriterBase
    {
        public AlwaysTrueColumnWriter(NpgsqlDbType dbType = NpgsqlDbType.Bit) : base(dbType)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return true;
        }
    }
}