using System.Text.Json.Serialization;

namespace Nexus.Library;


public class DataMessage
{
    public string? ExtraInfo { get; set; }
    public string? Data { get; set; }

    private Dictionary<string, string>? _metaData;

    [JsonIgnore]
    public Dictionary<string, string> Metadata => _metaData ??= ExtraInfo.ToDictionary();
}