using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.PostgreSQL.Configuration.IntegrationTests.Support;
using Serilog.Sinks.PostgreSQL.IntegrationTests;
using Xunit;

namespace Serilog.Sinks.PostgreSQL.Configuration.IntegrationTests
{
    public class ConfigurationSettingsDbWriteTests
    {
        private const string _connectionString = "User ID=serilog;Password=serilog;Host=localhost;Port=55432;Database=serilog_logs";

        private DbHelper _dbHelper = new DbHelper(_connectionString);

        [Fact]
        public void ConfigurationFromJson_ShouldCreateTableAndInsertValues()
        {
            var tableName = "table_from_config";

            _dbHelper.RemoveTable(tableName);

            var jsonConfig = @"{
                      ""Serilog"": {
                        ""Using"": [ ""Serilog.Sinks.PostgreSQL.Configuration"" ],
                        ""MinimumLevel"": ""Debug"",
                        ""WriteTo"": [
                          {
                            ""Name"": ""PostgreSQL"",
                            ""Args"": {
                              ""connectionString"": """ + _connectionString +  @""",
                              ""tableName"": """+ tableName + @""",
                              ""needAutoCreateTable"": true
                            }
                          }
                        ]
                      },
                      ""Columns"": {
                        ""message"": ""RenderedMessageColumnWriter"",
                        ""level"": {
                          ""Name"": ""LevelColumnWriter"",
                          ""Args"": {
                            ""renderAsText"": true,
                            ""dbType"": ""Varchar""
                          }
                        }
                      }
                    }";

            var configuration = new ConfigurationBuilder()
                .AddJsonFromString(jsonConfig)
                //.AddJsonFile("appsettings.json")
                .Build();


            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            var logger = loggerConfig
                .CreateLogger();

            logger.Information("Hello, world!");

            logger.Dispose();

            var actualRowsCount = _dbHelper.GetTableRowsCount(tableName);

            Assert.Equal(1, actualRowsCount);
        }


        [Fact]
        public void GetConnectionStringByKey_ShouldCreateTableAndInsertValues()
        {
            var tableName = "table_from_config2";

            _dbHelper.RemoveTable(tableName);

            var jsonConfig = @"{
                      ""Serilog"": {
                        ""Using"": [ ""Serilog.Sinks.PostgreSQL.Configuration"" ],
                        ""MinimumLevel"": ""Debug"",
                        ""WriteTo"": [
                          {
                            ""Name"": ""PostgreSQL"",
                            ""Args"": {
                              ""connectionString"": ""LogsDb"",
                              ""tableName"": """ + tableName + @""",
                              ""needAutoCreateTable"": true
                            }
                          }
                        ]
                      },
                      ""Columns"": {
                        ""message"": ""RenderedMessageColumnWriter"",
                        ""level"": {
                          ""Name"": ""LevelColumnWriter"",
                          ""Args"": {
                            ""renderAsText"": true,
                            ""dbType"": ""Varchar""
                          }
                        }
                      },
                    ""ConnectionStrings"": {
                        ""LogsDb"": """+ _connectionString +@"""
                      }
                    }";

            var configuration = new ConfigurationBuilder()
                .AddJsonFromString(jsonConfig)
                //.AddJsonFile("appsettings.json")
                .Build();


            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            var logger = loggerConfig
                .CreateLogger();

            logger.Information("Hello, world!");

            logger.Dispose();

            var actualRowsCount = _dbHelper.GetTableRowsCount(tableName);

            Assert.Equal(1, actualRowsCount);
        }


        [Fact]
        public void ColumnsSectionIsNestedAndPathProvided_ShouldCreateTableAndInsertValues()
        {
            var tableName = "table_from_config3";

            _dbHelper.RemoveTable(tableName);

            var jsonConfig = @"{
                      ""Serilog"": {
                        ""Using"": [ ""Serilog.Sinks.PostgreSQL.Configuration"" ],
                        ""MinimumLevel"": ""Debug"",
                        ""WriteTo"": [
                          {
                            ""Name"": ""PostgreSQL"",
                            ""Args"": {
                              ""connectionString"": """ + _connectionString + @""",
                              ""tableName"": """ + tableName + @""",
                              ""configurationPath"": ""ColumnsParentSection:SubSection"",
                              ""needAutoCreateTable"": true
                            }
                          }
                        ]
                      },
                      ""ColumnsParentSection"" : {
                        ""SubSection"" : {
                              ""Columns"": {
                                ""message"": ""RenderedMessageColumnWriter"",
                                ""level"": {
                                  ""Name"": ""LevelColumnWriter"",
                                  ""Args"": {
                                    ""renderAsText"": true,
                                    ""dbType"": ""Varchar""
                                  }
                                }
                             }
                        }
                      }
                    }";

            var configuration = new ConfigurationBuilder()
                .AddJsonFromString(jsonConfig)
                //.AddJsonFile("appsettings.json")
                .Build();


            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            var logger = loggerConfig
                .CreateLogger();

            logger.Information("Hello, world!");

            logger.Dispose();

            var actualRowsCount = _dbHelper.GetTableRowsCount(tableName);

            Assert.Equal(1, actualRowsCount);
        }


        [Fact]
        public void NoColumnsSectionIsPresent_ShouldThrow()
        {
            var tableName = "table_from_config4";

            _dbHelper.RemoveTable(tableName);

            var jsonConfig = @"{
                      ""Serilog"": {
                        ""Using"": [ ""Serilog.Sinks.PostgreSQL.Configuration"" ],
                        ""MinimumLevel"": ""Debug"",
                        ""WriteTo"": [
                          {
                            ""Name"": ""PostgreSQL"",
                            ""Args"": {
                              ""connectionString"": """ + _connectionString + @""",
                              ""tableName"": """ + tableName + @""",
                              ""configurationPath"": ""ColumnsParentSection:SubSection"",
                              ""needAutoCreateTable"": true
                            }
                          }
                        ]
                      },
                     ""ColumnsParentSection"" : {
                        ""SubSection"" : {
                        }
                      }
                    }";

            var configuration = new ConfigurationBuilder()
                .AddJsonFromString(jsonConfig)
                //.AddJsonFile("appsettings.json")
                .Build();

            //TODO: check exception type: Serilog throws expected InvalidOperationException wrapped in TargetInvocationException
            Assert.ThrowsAny<Exception>(() =>
            {
                var loggerConfig = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration);
            });


        }

       
    }
}
