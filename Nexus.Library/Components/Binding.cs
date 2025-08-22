using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nexus.Library.Components;

public abstract class Binding : Component
{
    [JsonConstructor]
    protected Binding()
    {
    }

    protected Binding(ILogger logger) : base(logger)
    {
    }
}