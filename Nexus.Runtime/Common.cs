using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Protobuf.Collections;
using GrpcCaller;
using Nexus.Library;

namespace Nexus.Runtime;

public static class Common
{
    public static string ExpandToString<T, TR>(this Dictionary<T, TR> dict) where T : notnull =>
        string.Join(";", dict.Select(x => $"{x.Key}={x.Value}"));

    public static string ToSeparatedString(this HttpResponseHeaders headers)
    {
        var pairs = headers.Select(pair => $"{pair.Key}={pair.Value.FirstOrDefault()}");
        return string.Join(";", pairs);
    }
    
    public static Dictionary<string, string> ToDictionary(this string? data)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(data))
            return result;

        var matches = Regex.Matches(data, @"(?<key>[^=]+)=(?<value>[^;]+)");
        foreach (Match match in matches)
            result.Add(match.Groups[1].Value, match.Groups[2].Value);

        return result;
    }

    public static string ExpandToString<T>(this List<T> list) =>
        string.Join(", ", list);

    public static void SkipTo(this string[] content, string term, ref int position)
    {
        while (content[position] != term)
            position++;
    }

    public static JsonNamingPolicy GetJsonNamingPolicy(this string name) =>
        name.ToLowerInvariant() switch
        {
            "snakecase" => JsonNamingPolicy.SnakeCaseLower,
            "kebabcase" => JsonNamingPolicy.KebabCaseLower,
            _ => JsonNamingPolicy.CamelCase
        };

    public static void AddHeaders(this Dictionary<string, string> headers, HttpRequestHeaders requestHeaders)
    {
        foreach (var header in headers!)
        {
            switch (header.Key)
            {
                case "Content-Type":
                    requestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(header.Value));
                    break;
                case "Accept-Encoding":
                    requestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse(header.Value));
                    break;
                default:
                    requestHeaders.Add(header.Key, header.Value);
                    break;
            }
        }
    }

    public static DataMessage ToDataMessage(this QueryRequest request)
    {
        return new DataMessage
        {
            Data = request.Data,
            ExtraInfo = request.ExtraInfo.ToDictionary(),
        };
    }
    
    public static QueryRequest ToQueryRequest(this DataMessage message)
    {
        var request = new QueryRequest
        {
            Data = message.Data
        };
        message.ExtraInfo.ToMap(request.ExtraInfo);
        return request;
    }

    public static Dictionary<T, TR> ToDictionary<T,TR>(this MapField<T, TR> map) where T : notnull
    {
        return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
    
    public static void ToMap<T,TR>(this Dictionary<T, TR>? dict, MapField<T, TR> map) where T : notnull
    {
        map.Clear();
        if (dict is null || dict.Count == 0)
            return;
        
        foreach (var kvp in dict)
        {
            map.Add(kvp.Key, kvp.Value);
        }
    }
}

