namespace Permit.IO.Demo;

public class PermitAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermitAuthorizationMiddleware> _logger;

    public PermitAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<PermitAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPermitService permitService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var permitAttributes = endpoint.Metadata
            .GetOrderedMetadata<PermitAuthorizeAttribute>();

        if (!permitAttributes.Any())
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User not authenticated for Permit check");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        try
        {
            // Criar UserKey a partir dos claims do token
            var userKey = permitService.CreateUserKeyFromClaims(context.User);

            _logger.LogInformation("Checking permissions for user: {UserEmail}", userKey.email);

            // Verificar todas as permissões requeridas
            foreach (var attribute in permitAttributes)
            {
                var resourceId = attribute.UseRouteId
                    ? context.Request.RouteValues["id"]?.ToString()
                    : null;

                var permitted = await permitService.CheckPermissionAsync(
                    userKey,
                    attribute.Action,
                    attribute.Resource,
                    resourceId
                );

                if (!permitted)
                {
                    _logger.LogWarning(
                        "Permission denied: User={UserEmail}, Action={Action}, Resource={Resource}",
                        userKey.email, attribute.Action, attribute.Resource);

                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Forbidden",
                        message = $"User '{userKey.email}' is not permitted to {attribute.Action} {attribute.Resource}"
                    });
                    return;
                }
            }

            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create UserKey from claims");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid user claims" });
        }
    }
}

// Extension method para facilitar registro
public static class PermitAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UsePermitAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermitAuthorizationMiddleware>();
    }
}
