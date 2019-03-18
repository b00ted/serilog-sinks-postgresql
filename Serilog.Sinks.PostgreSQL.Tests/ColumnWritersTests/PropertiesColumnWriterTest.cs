using System;
using System.Linq;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sinks.PostgreSQL.Tests
{
    public class PropertiesColumnWriterTest
    {
        [Fact]
        public void NoProperties_ShouldReturnEmptyJsonObject()
        {
            var writer = new PropertiesColumnWriter();

            var testEvent = new LogEvent(DateTime.Now, LogEventLevel.Debug, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>());

            var result = writer.GetValue(testEvent);

            Assert.Equal("{}", result);
        }

        [Fact]
        public void OneProperty_ShouldReturnJsonWithOneProperty()
        {
            var writer = new PropertiesColumnWriter();

            var testEvent = new LogEvent(DateTime.Now, LogEventLevel.Debug, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), new[] { new LogEventProperty("Prop", new ScalarValue(1)) });

            var result = writer.GetValue(testEvent);

            Assert.Equal(@"{""Prop"":1}", result);
        }

        [Fact]
        public void TwoProperties_ShouldReturnJsonWithTwoProperties()
        {
            var writer = new PropertiesColumnWriter();

            var properties = new[] { new LogEventProperty("Prop1", new ScalarValue(1)), new LogEventProperty("Prop2", new ScalarValue(2)) };
            var testEvent = new LogEvent(DateTime.Now, LogEventLevel.Debug, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), properties);

            var result = writer.GetValue(testEvent);

            Assert.Equal(@"{""Prop1"":1, ""Prop2"":2}", result);
        }

        [Fact]
        public void OnlyExcludedProperty_ShouldReturnEmptyJsonObject()
        {
            var writer = new PropertiesColumnWriter(exclude: new[] { "Prop" });

            var testEvent = new LogEvent(DateTime.Now, LogEventLevel.Debug, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), new[] { new LogEventProperty("Prop", new ScalarValue(1)) });

            var result = writer.GetValue(testEvent);

            Assert.Equal("{}", result);
        }
    }
}