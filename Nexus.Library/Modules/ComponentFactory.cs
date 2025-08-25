using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Nexus.Library.Components;

namespace Nexus.Library.Modules;

public static class ComponentFactory
{
    private static readonly Dictionary<string, Type> _components = new();

    public static List<string> ComponentNames => _components.Keys.ToList();

    public static List<Assembly> GetAllAssemblies()
    {
        var assemblies = AssemblyLoadContext.Default.Assemblies;
        return assemblies.ToList();
    }

    public static void Register(ILogger? logger)
    {
        _components.Clear();
        var assemblies = GetAllAssemblies();
        foreach (var asm in assemblies)
        {
            try
            {
                var types = asm.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsSubclassOf(typeof(Component)) && !type.IsAbstract)
                    {
                        try
                        {
                            logger?.LogInformation("Registering component: {TypeName}", type.Name);
                            var component = (Component)Activator.CreateInstance(type, logger)!;
                            _components.Add(component.Type!, type);
                            logger?.LogInformation("Component registered: {ComponentType}", component.Type);
                        }
                        catch (Exception e)
                        {
                            logger?.LogError(e, "Error while registering component: {TypeName}", type.Name);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }

    public static void AddComponent(Type component)
    {
        _components.Add(component.Name, component);
    }

    public static T GetComponent<T>(string type, ILogger logger) where T : Component
    {
        return (T)Activator.CreateInstance(_components[type], logger)!;
    }

    public static Type GetType(string type) => _components[type];

    public static Dictionary<string, Type> GetTypeMappings() => _components;
}