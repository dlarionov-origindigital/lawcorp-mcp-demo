using LawCorp.Mcp.Core.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Registers Entra ID authentication, OBO token exchange, and identity resolution
/// services. Called from <c>Program.cs</c> when <c>UseAuth=true</c>.
/// <para>
/// See <see href="../../../docs/auth-config.md">docs/auth-config.md</see> for
/// Azure app registration and appsettings setup instructions.
/// </para>
/// </summary>
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddEntraIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        services.AddHttpContextAccessor();
        services.AddAuthorization();

        services.AddScoped<IUserContext>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            return accessor.HttpContext?.Items[UserContextResolutionMiddleware.UserContextKey] as IUserContext
                ?? throw new UnauthorizedAccessException(
                    "Authenticated user context was not resolved. " +
                    "Ensure the request includes a valid Entra ID Bearer token and the " +
                    "user's EntraObjectId is registered. " +
                    "See docs/auth-config.md for setup instructions.");
        });

        services.AddScoped<IFirmIdentityContext>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            return accessor.HttpContext?.Items[UserContextResolutionMiddleware.FirmIdentityKey] as IFirmIdentityContext
                ?? throw new UnauthorizedAccessException(
                    "Firm identity context was not resolved. " +
                    "Ensure the request includes a valid Entra ID Bearer token and the " +
                    "user's EntraObjectId is registered in the Attorney table. " +
                    "See docs/auth-config.md for setup instructions.");
        });

        services.AddScoped<IDownstreamTokenProvider, OboDownstreamTokenProvider>();

        return services;
    }
}
