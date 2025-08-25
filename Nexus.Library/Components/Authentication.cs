using System.Text;
using System.Text.Json.Serialization;

namespace Nexus.Library.Components;

public enum AuthType
{
    None,
    Basic,
    Token
}

public class Authentication
{
    public AuthType AuthType { get; set; } = AuthType.None;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string LoginUrl { get; set; } = "";
    [JsonIgnore]
    public string Token { get; set; } = "";

    public string GetAuthHeader()
    {
        switch (AuthType)
        {
            case AuthType.Basic:
                var userpass = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
                return $"Authorization: Basic {userpass}";
            case AuthType.Token:
                return $"Authorization: Bearer {Token}";    
            
            default:
                return "";
        }
    }
}