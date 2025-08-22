using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexus.Client;

public static class Common
{
    public static string ExpandToString<T, TR>(this Dictionary<T, TR> dict) where T : notnull =>
        string.Join(";", dict.Select(x => $"{x.Key}={x.Value}"));

    public static string ToSeparatedString(this HttpResponseHeaders headers)
    {
        var pairs = headers.Select(pair => $"{pair.Key}={pair.Value.FirstOrDefault()}");
        return string.Join(";", pairs);
    }

    public static ExtraInfo ToDictionary(this string? data)
    {
        var result = new ExtraInfo();

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

    public static void AddHeaders(this ExtraInfo headers, HttpRequestHeaders requestHeaders)
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
}

public class ExtraInfo : Dictionary<string, string>
{

}