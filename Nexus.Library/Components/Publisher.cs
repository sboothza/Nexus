using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nexus.Library.Components;

public abstract class Publisher : Component
{
    [JsonConstructor]
    protected Publisher()
    {
    }

    protected Publisher(ILogger logger) : base(logger)
    {
    }
}