using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using WebApiPaseto.Service;

namespace WebApiPaseto;

public class PasetoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly PasetoService _pasetoService;

    public PasetoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        PasetoService pasetoService)
        : base(options, logger, encoder)
    {
        _pasetoService = pasetoService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Log all headers for debugging
        Logger.LogInformation("=== Authentication Handler Called ===");
        Logger.LogInformation("Request Path: {Path}", Request.Path);
        Logger.LogInformation("Headers Count: {Count}", Request.Headers.Count);
        foreach (var header in Request.Headers)
        {
            Logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
        }

        if (!Request.Headers.ContainsKey("Authorization"))
        {
            Logger.LogWarning("Authorization header is missing!");
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            Logger.LogInformation("Authorization Header Value: {AuthHeader}", authHeader);

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Authorization header doesn't start with 'Bearer '");
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            Logger.LogInformation("Extracted Token: {Token}", token);

            var validationResult = _pasetoService.ValidateToken(token);

            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Token validation failed");
                return Task.FromResult(AuthenticateResult.Fail("Invalid Token"));
            }

            Logger.LogInformation("Token validated successfully");

            var claims = new List<Claim>();

            // Parse the payload as JSON
            var payload = JObject.Parse(validationResult.Paseto.RawPayload);

            if (payload["sub"] != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, payload["sub"]!.ToString()));
            }

            if (payload["email"] != null)
            {
                claims.Add(new Claim("email", payload["email"]!.ToString()));
            }

            if (payload["role"] != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, payload["role"]!.ToString()));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Token validation exception");
            return Task.FromResult(AuthenticateResult.Fail($"Token validation failed: {ex.Message}"));
        }
    }
}
