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