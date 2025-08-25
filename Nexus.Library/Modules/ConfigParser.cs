using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Nexus.Library.Components;
using YamlDotNet.Serialization;

namespace Nexus.Library.Modules;

public static class ConfigParser
{
    private static IDeserializer? _deserializerInstance;

    private static IDeserializer _deserializer => _deserializerInstance ??= new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    public static Component? ParseComponent(string configContent, ILogger logger, Meter meter, Manager manager)
    {
        try
        {
            logger.LogInformation("Parsing component config");
            var obj = _deserializer.Deserialize<Component>(configContent);
            var type = obj.Type;
            logger.LogInformation("Component found: {ObjName}", obj.Name);
            var componentType = ComponentFactory.GetType(type!);
            var component = (Component)_deserializer.Deserialize(configContent, componentType)!;
            component.SetLogger(logger);
            component.CreateMetrics(meter);
            component.Configure(manager);
            logger.LogInformation("Component created: {ComponentName}", component.Name);
            return component;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while parsing component config");
            return null;
        }
    }

    public static List<Component?> ParseComponentFolder(string folder, ILogger logger, Meter meter, Manager manager)
    {
        var components = new List<Component?>();
        foreach (var file in Directory.GetFiles(folder, "*.yaml"))
        {
            logger.LogInformation("Parsing component file: {File}", file);
            var yaml = File.ReadAllText(file);
            var component = ParseComponent(yaml, logger, meter, manager);
            components.Add(component);
        }

        return components;
    }
}