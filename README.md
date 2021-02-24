# Serilog.Sinks.Postgresql
A [Serilog](https://github.com/serilog/serilog) sink that writes to PostgreSQL

**Package** - [Serilog.Sinks.PostgreSQL](https://www.nuget.org/packages/Serilog.Sinks.PostgreSQL/)
| **Platforms** - .NET 4.5, .NET Standard 2.0

#### Code

```csharp
string connectionstring = "User ID=serilog;Password=serilog;Host=localhost;Port=5432;Database=logs";

string tableName = "logs";

//Used columns (Key is a column name) 
//Column type is writer's constructor parameter
IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
{
    {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
    {"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
    {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
    {"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
    {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
    {"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
    {"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
    {"machine_name", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") }
};

var logger = new LoggerConfiguration()
			        .WriteTo.PostgreSQL(connectionstring, tableName, columnWriters)
			        .CreateLogger();
```

##### Table auto creation
If you set parameter `needAutoCreateTable` to `true` sink automatically create table.
You can change column sizes by setting values in `TableCreator` class:
```csharp
//Sets size of all BIT and BIT VARYING columns to 20
TableCreator.DefaultBitColumnsLength = 20;

//Sets size of all CHAR columns to 30
TableCreator.DefaultCharColumnsLength = 30;

//Sets size of all VARCHAR columns to 50
TableCreator.DefaultVarcharColumnsLength = 50;
```

or set column size individually by providing `columnLength` parameter to ColumnWriter
```charp
int columnLength = 100;
new LevelColumnWriter(true, NpgsqlDbType.Varchar, columnLength)
```

##### Mixed case table or column names
If your schema, table or column names is in mixed case, you should set parameter `respectCase` to `true`.


### Configuration
Sink support configuration from json file via separate package [Serilog.Sinks.PostgreSQL.Configuration](https://www.nuget.org/packages/Serilog.Sinks.PostgreSQL.Configuration/)

Example of config
```js
"Serilog": {
    "Using": [ "Serilog.Sinks.PostgreSQL.Configuration" ],
    "MinimumLevel": "Debug",
    "Enrich": [ "WithMachineName" ],
    "WriteTo": [
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "LogsDb",
          "tableName": "table_name",
          "needAutoCreateTable": true
        }
      }
    ]
  },
  "ConnectionStrings": {
    "LogsDb": "User ID=serilog;Password=serilog;Host=localhost;Port=55432;Database=serilog_logs"
  },
  "Columns": {
    "message": "RenderedMessageColumnWriter",
    "message_template": "MessageTemplateColumnWriter",
    "level": {
      "Name": "LevelColumnWriter",
      "Args": {
        "renderAsText": true,
        "dbType": "Varchar"
      }
    },
    "raise_date": "TimestampColumnWriter",
    "exception": "ExceptionColumnWriter",
    "properties": "LogEventSerializedColumnWriter",
    "props_test": {
      "Name": "PropertiesColumnWriter",
      "Args": { "dbType": "Json" }
    },
    "machine_name": {
      "Name": "SinglePropertyColumnWriter",
      "Args": {
        "propertyName": "MachineName",
        "writeMethod": "Raw"
      }
    }
  }
}
```
##### Using section

You should add ```Serilog.Sinks.PostgreSQL.Configuration``` into ```Using``` section array


##### Connection strings

```connectionString``` can be either connection string itself or the name of connection string in ```ConnectionStrings``` section

##### Columns

If ```Columns``` section is a subsection of other you should provide a path to it in a ```configurationPath``` parameter e.g.
```js
"Serilog": {
    "Using": [ "Serilog.Sinks.PostgreSQL.Configuration" ],
    "MinimumLevel": "Debug",
    "Enrich": [ "WithMachineName" ],
    "WriteTo": [
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "User ID=serilog;Password=serilog;Host=localhost;Port=55432;Database=serilog_logs",
          "tableName": "table_name",
          "configurationPath": "ColumnsSectionHolder:AnotherSubSection"
          "needAutoCreateTable": true
        }
      }
    ]
  },  
  "ColumnsSectionHolder": {
      "AnotherSubSection" : {
        "Columns": {
            "message": "RenderedMessageColumnWriter",
            "level": {
                "Name": "LevelColumnWriter",
                "Args": {
                    "renderAsText": true,
                    "dbType": "Varchar"
                }
            },
            "raise_date": "TimestampColumnWriter",
            "exception": "ExceptionColumnWriter"
        }
      }
  }  
```

In columns section key is a column name and value is a column writer. Writer can be written in two ways:
1. As string value - this mean a parameterless constructor invocation e.g. this lines in config
```js
"Columns" : {
    "message": "RenderedMessageColumnWriter"
}
```
is similar to this code
```csharp
IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
{
    {"message", new RenderedMessageColumnWriter() }
};
```
2. As object - then writer's class name is located by the ```Name``` key and constructor arguments by the ```Args``` key. e.g.
```js
"Columns" : {
    "level": {
                "Name": "LevelColumnWriter",
                "Args": {
                    "renderAsText": true,
                    "dbType": "Varchar"
                }
            }
}
```
is similar to this code
```csharp
IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
{
    {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) }
};
```

##### Non builtin writers

At the moment package supports non bultin column writers only from executing asembly.