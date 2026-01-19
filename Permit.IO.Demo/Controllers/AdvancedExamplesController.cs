using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Permit.IO.Demo.Controllers;

[ApiController]
[Route("api/examples")]
[Authorize]
public class AdvancedExamplesController : ControllerBase
{
    private readonly IPermitService _permitService;

    public AdvancedExamplesController(IPermitService permitService)
    {
        _permitService = permitService;
    }

    // Exemplo: Acesso condicional baseado em múltiplas verificações
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.Identity!.Name!;

        var permissions = new Dictionary<string, bool>
        {
            ["canViewReports"] = await _permitService.CheckPermissionAsync(userId, "read", "report"),
            ["canViewUsers"] = await _permitService.CheckPermissionAsync(userId, "read", "user"),
            ["canManageSettings"] = await _permitService.CheckPermissionAsync(userId, "update", "settings")
        };

        // Retorna dashboard personalizado baseado nas permissões
        return Ok(new
        {
            user = userId,
            permissions,
            availableModules = permissions
                .Where(p => p.Value)
                .Select(p => p.Key)
                .ToList()
        });
    }

    // Exemplo: Filtrar dados baseado em permissões
    [HttpGet("documents")]
    public async Task<IActionResult> GetFilteredDocuments()
    {
        var userId = User.Identity!.Name!;

        var allDocuments = new[]
        {
            new { Id = "1", Title = "Public Doc", Type = "public" },
            new { Id = "2", Title = "Confidential Doc", Type = "confidential" }
        };

        // Verificar permissão para cada documento
        var visibleDocuments = new List<object>();

        foreach (var doc in allDocuments)
        {
            var canView = await _permitService.CheckPermissionAsync(
                userId,
                "read",
                "document",
                doc.Id
            );

            if (canView)
            {
                visibleDocuments.Add(doc);
            }
        }

        return Ok(visibleDocuments);
    }
}