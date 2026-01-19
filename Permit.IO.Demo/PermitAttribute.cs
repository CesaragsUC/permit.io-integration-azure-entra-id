using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Permit.IO.Demo;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class PermitAuthorizeAttribute : Attribute
{
    public string Action { get; }
    public string Resource { get; }
    public bool UseRouteId { get; set; } = false; // Para recursos específicos

    public PermitAuthorizeAttribute(string action, string resource)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }
}

public class PermitAuthorizeFilter : IAsyncAuthorizationFilter
{
    private readonly IPermitService _permitService;

    public PermitAuthorizeFilter(IPermitService permitService)
    {
        _permitService = permitService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Pega o atributo do endpoint
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<PermitAuthorizeAttribute>()
            .FirstOrDefault();

        if (attribute == null) return;

       if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
       {
           context.Result = new UnauthorizedResult();
           await Task.CompletedTask;
           return;
       }

        var userKey = _permitService.CreateUserKeyFromClaims(context.HttpContext.User);

        var allowed = await _permitService.CheckPermissionAsync(
            userKey,
            attribute.Action,
            attribute.Resource);

        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}