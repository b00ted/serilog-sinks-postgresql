using System;
using System.Linq;
using NpgsqlTypes;
using Xunit;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.PostgreSQL.Tests
{
    public class TimestampColumnWriterTest
    {
        [Fact]
        public void ByDefault_ShouldReturnTimestampValueWithTimezone()
        {
            var writer = new TimestampColumnWriter();

            var timeStamp = new DateTimeOffset(2017,8,13,11,11,11, new TimeSpan());

            var testEvent = new LogEvent(timeStamp, LogEventLevel.Debug, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

            var result = writer.GetValue(testEvent);

            Assert.Equal(timeStamp, result);
        }

        [Fact]
        public void DbTypeWithTimezoneSelected_ShouldReturnTimestampValue()
        {
            var writer = new TimestampColumnWriter(NpgsqlDbType.TimestampTZ);

            var timeStamp = new DateTimeOffset(2017, 8, 13, 11, 11, 11, new TimeSpan());

            var testEvent = new LogEvent(timeStamp, LogEventLevel.Debug, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

            var result = writer.GetValue(testEvent);

            Assert.Equal(timeStamp, result);
        }
    }
}
