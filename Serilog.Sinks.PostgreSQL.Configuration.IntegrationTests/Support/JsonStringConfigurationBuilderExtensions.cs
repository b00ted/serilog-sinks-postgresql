using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Serilog.Sinks.PostgreSQL.Configuration.IntegrationTests.Support
{
    public static class JsonStringConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddJsonFromString(this IConfigurationBuilder builder, string jsonString)
        {
            var stream = CreateStreamFromString(jsonString);

            return builder.AddJsonStream(stream);
        }

        private static Stream CreateStreamFromString(string aString)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(aString));
        }
    }
}