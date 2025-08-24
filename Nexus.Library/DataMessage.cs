using System.Text.Json.Serialization;

namespace Nexus.Library;


public class DataMessage
{
    public Dictionary<string, string>? ExtraInfo { get; set; } = new();
    public string? Data { get; set; }
}