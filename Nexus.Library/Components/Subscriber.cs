using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nexus.Library.Components;

public abstract class Subscriber : Component
{
    public string? BindingName { get; set; }

    [JsonConstructor]
    protected Subscriber()
    {
    }

    protected Subscriber(ILogger logger) : base(logger)
    {
    }
}