using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.PostgreSQL;

namespace Serilog
{
    public class ConfigurationReader
    {
        public const string ColumnsSectionName = "Columns";

        private readonly IConfiguration _configuration;

        public ConfigurationReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDictionary<string, ColumnWriterBase> GetColumnOptions(string configurationPath)
        {
            IConfiguration rootSection = _configuration;

            if (!String.IsNullOrEmpty(configurationPath))
            {
                rootSection = _configuration.GetSection(configurationPath);
            }
            
            //TODO: Add Usigns section
            var assemblies = GetAssemblies();
            var columnWriterTypes = assemblies.SelectMany(a => a.ExportedTypes)
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ColumnWriterBase)))
                .ToList();

            IConfigurationSection columnsSection = rootSection.GetSection(ColumnsSectionName);
            if (!columnsSection.Exists())
            {
                throw new InvalidOperationException($"'{ColumnsSectionName}' section not found in provided configuration path: {configurationPath}");
            }

            var columnOptions = new Dictionary<string, ColumnWriterBase>();

            foreach (IConfigurationSection columnSection in columnsSection.GetChildren())
            {
                string columnName = columnSection.Key;

                ColumnWriterBase columnWriter = GetColumnWriter(columnSection, columnWriterTypes);

                columnOptions[columnName] = columnWriter;
            }

            return columnOptions;
        }

        private ColumnWriterBase GetColumnWriter(IConfigurationSection columnSection, List<Type> columnWriterTypes)
        {
            //Parameterless constructor
            if (columnSection.Value != null)
            {
                Type columnWriterType = columnWriterTypes.FirstOrDefault(t =>
                    t.Name == columnSection.Value &&
                    t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                    .Any(c => c.GetParameters().Length == 0 || c.GetParameters().All(p => p.HasDefaultValue)));

                if (columnWriterType != null)
                {
                    (ConstructorInfo ctor, var parameters) = GetParameterlessConstructor(columnWriterType);

                    return (ColumnWriterBase)ctor.Invoke(parameters);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot create writer of type {columnSection.Value} for column {columnSection.Key}");
                }
            }
            else
            {
                IConfigurationSection nameSection = columnSection.GetSection("Name");
                string typeName = nameSection.Value;
                if (String.IsNullOrEmpty(typeName))
                {
                    throw new InvalidOperationException($"The configuration value in {nameSection.Path} has no 'Name' element.");
                }

                IConfigurationSection argsSection = columnSection.GetSection("Args");
                (ConstructorInfo ctor, var parameters) = GetConstructor(typeName, argsSection, columnWriterTypes);

                return (ColumnWriterBase)ctor.Invoke(parameters);
            }

            
        }

        private (ConstructorInfo, object[]) GetConstructor(string typeName, IConfigurationSection argsSection, List<Type> columnWriterTypes)
        {
            var parametersSectionsDict = argsSection.GetChildren().ToDictionary(c => c.Key);

            //            var parameterNames = argsSection.GetChildren().Select(c => c.Key);

            var candidateConstructors = columnWriterTypes.Where(t => t.Name == typeName)
                .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                .Where(c => c.GetParameters().All(p => p.HasDefaultValue || parametersSectionsDict.ContainsKey(p.Name)));

            ConstructorInfo ctor = candidateConstructors
                .OrderByDescending(c => c.GetParameters().Count(p => parametersSectionsDict.ContainsKey(p.Name)))
                .FirstOrDefault();

            if (ctor == null)
            {
                throw new InvalidOperationException($"Cannot create writer of type {typeName}: no suitable constructors found");
            }

            var parameters = new List<object>(ctor.GetParameters().Length);
            foreach (ParameterInfo parameter in ctor.GetParameters())
            {
                if (parametersSectionsDict.ContainsKey(parameter.Name))
                {
                    parameters.Add(parametersSectionsDict[parameter.Name].Get(parameter.ParameterType));
                }
                else
                {
                    parameters.Add(parameter.DefaultValue);
                }
            }

            return (ctor, parameters.ToArray());
        }

        private (ConstructorInfo, object[]) GetParameterlessConstructor(Type columnWriterType)
        {
            var parameterlessCtor = columnWriterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0);
            if (parameterlessCtor != null)
            {
                return (parameterlessCtor, new object[] { });
            }

            var ctorWithDefaultParams = columnWriterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(c => c.GetParameters().All(p => p.HasDefaultValue))
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctorWithDefaultParams != null)
            {
                return (ctorWithDefaultParams, ctorWithDefaultParams.GetParameters().Select(p => p.DefaultValue).ToArray());
            }

            throw new InvalidOperationException($"Cannot find constructor for type {columnWriterType.FullName}");
        }

        private List<Assembly> GetAssemblies()
        {
            var assemblies = new List<Assembly> { typeof(PostgreSQLSink).Assembly, Assembly.GetEntryAssembly() };

            return assemblies;
        }
    }
}