namespace Nexus.Library;


public class DataMessage
{
    public bool Success { get; set; }
    public Dictionary<string, string>? ExtraInfo { get; set; } = new();
    public string? Data { get; set; }
}

public class ErrorMessage : DataMessage
{
    public string? Error { get; set; }
    public string? StackTrace { get; set; }
}