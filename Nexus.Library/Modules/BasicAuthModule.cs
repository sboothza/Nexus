using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexus.Library.Modules;

public class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> optionsMonitor,
    UrlEncoder urlEncoder,
    ILoggerFactory loggerFactory, Func<string, string, Task<IdentityUser?>> validateMemberCallback)
    : AuthenticationHandler<AuthenticationSchemeOptions>(optionsMonitor, loggerFactory,
        urlEncoder)
{
    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues value))
            return AuthenticateResult.Fail("Missing Authorization header");
        IdentityUser? member;
        try
        {
            if (!AuthenticationHeaderValue.TryParse(value, out var authenticationHeader))
                return AuthenticateResult.Fail("Invalid Header Format");

            var credentialBytes = Convert.FromBase64String(authenticationHeader.Parameter!);
            var credential = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

            if (credential.Length != 2)
                return AuthenticateResult.Fail("Invalid Header Content");

            var userName = credential[0];
            var password = credential[1];

             member = await validateMemberCallback(userName, password);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex.Message);
        }

        if (member == null)
            return AuthenticateResult.Fail("Invalid Member");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, member.Id), new Claim(ClaimTypes.Name, member.UserName!),
        };

        var claimIdentity = new ClaimsIdentity(claims, Scheme.Name);
        var claimPrincipal = new ClaimsPrincipal(claimIdentity);
        var ticket = new AuthenticationTicket(claimPrincipal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}