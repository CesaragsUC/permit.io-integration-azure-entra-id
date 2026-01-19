using PermitSDK.Models;
using System.Security.Claims;

namespace Permit.IO.Demo;

public interface IPermitService
{
    Task<bool> CheckPermissionAsync(string userId, string action, string resource, string? resourceId = null);
    Task<bool> CheckPermissionAsync(UserKey user, string action, string resource, string? resourceId = null);
    UserKey CreateUserKeyFromClaims(ClaimsPrincipal user);
}

public class PermitService : IPermitService
{
    private readonly PermitSDK.Permit _permit;
    private readonly ILogger<PermitService> _logger;

    public PermitService(IConfiguration configuration, ILogger<PermitService> logger)
    {
        _logger = logger;

        var clientToken = configuration["Permit:ApiKey"]
            ?? throw new InvalidOperationException("Permit API Key not configured");

        var pdpUrl = configuration["Permit:PdpUrl"] ?? "http://localhost:7766";

        _permit = new PermitSDK.Permit(clientToken, pdpUrl);
    }

    public async Task<bool> CheckPermissionAsync(string userId, string action, string resource, string? resourceId = null)
    {
        try
        {
            // Opção 1: UserKey simples (apenas o ID)
            var userKey = new UserKey(userId);

            // Opção 2: UserKey completo (se você tiver as informações)
            // var userKey = new UserKey(
            //     key: userId,
            //     firstName: "Cesar",      // extrair do token se disponível
            //     lastName: "Santos",      // extrair do token se disponível
            //     email: userId            // assumindo que userId é o email
            // );

            var resourceObj = string.IsNullOrEmpty(resourceId)
                ? new ResourceInput(resource)
                : new ResourceInput(resource, resourceId);

            var permitted = await _permit.Check(userKey, action, resourceObj);

            _logger.LogInformation(
                "Permission check: User={UserId}, Action={Action}, Resource={Resource}, ResourceId={ResourceId}, Result={Result}",
                userId, action, resource, resourceId ?? "null", permitted);

            return permitted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> CheckPermissionAsync(UserKey user, string action, string resource, string? resourceId = null)
    {
        try
        {
            var resourceObj = string.IsNullOrEmpty(resourceId)
                ? new ResourceInput(resource)
                : new ResourceInput(resource, resourceId);

            var permitted = await _permit.Check(user, action, resourceObj);

            _logger.LogInformation(
                "Permission check: User={UserKey}, Action={Action}, Resource={Resource}, ResourceId={ResourceId}, Result={Result}",
                user.key, action, resource, resourceId ?? "null", permitted);

            return permitted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserKey}", user.key);
            return false;
        }
    }

    // Método helper para criar UserKey a partir de Claims
    public UserKey CreateUserKeyFromClaims(ClaimsPrincipal user)
    {
        var email = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("upn")?.Value
            ?? throw new InvalidOperationException("User email not found in claims");

        var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value
            ?? user.FindFirst("given_name")?.Value;

        var lastName = user.FindFirst(ClaimTypes.Surname)?.Value
            ?? user.FindFirst("family_name")?.Value;

        return new UserKey(
            key: email,
            firstName: firstName,
            lastName: lastName,
            email: email
        );
    }
}