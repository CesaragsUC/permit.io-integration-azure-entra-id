
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Permit.IO.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação primeiro
public class AdminController : ControllerBase
{
    private readonly IPermitService _permitService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IPermitService permitService, ILogger<AdminController> logger)
    {
        _permitService = permitService;
        _logger = logger;
    }

    // Abordagem 1: Usando atributo (Declarativo - RECOMENDADO)
    [HttpGet]
    [PermitAuthorize("read", "document")]
    public IActionResult GetDocuments()
    {
        return Ok(new { message = "User can read documents" });
    }

    [HttpPost]
    [PermitAuthorize("create", "document")]
    public IActionResult CreateDocument([FromBody] DocumentDto document)
    {
        // Lógica de criação
        return Ok(new { message = "Document created" });
    }

    [HttpPut("{id}")]
    [PermitAuthorize("update", "document", UseRouteId = true)]
    public IActionResult UpdateDocument(string id, [FromBody] DocumentDto document)
    {
        // UseRouteId = true usa o {id} da rota para verificação granular
        return Ok(new { message = $"Document {id} updated" });
    }

    [HttpDelete("{id}")]
    [PermitAuthorize("delete", "document", UseRouteId = true)]
    public IActionResult DeleteDocument(string id)
    {
        return Ok(new { message = $"Document {id} deleted" });
    }

    // Abordagem 2: Verificação manual (quando precisa de lógica condicional)
    [HttpGet("conditional")]
    public async Task<IActionResult> ConditionalAccess()
    {
        var userId = User.Identity?.Name ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verificar múltiplas permissões
        var canRead = await _permitService.CheckPermissionAsync(userId, "read", "document");
        var canWrite = await _permitService.CheckPermissionAsync(userId, "write", "document");

        return Ok(new
        {
            canRead,
            canWrite,
            message = canWrite
                ? "Full access to documents"
                : canRead
                    ? "Read-only access"
                    : "No access"
        });
    }

    // Abordagem 3: Verificação granular por instância
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetDocumentDetails(string id)
    {
        var userId = User.Identity?.Name ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verificar permissão para documento específico
        var canView = await _permitService.CheckPermissionAsync(
            userId,
            "read",
            "document",
            id  // resourceId específico
        );

        if (!canView)
        {
            return Forbid();
        }

        // Lógica para buscar documento
        return Ok(new { id, content = "Document content..." });
    }

    // Múltiplas permissões no mesmo endpoint
    [HttpPost("bulk-delete")]
    [PermitAuthorize("delete", "document")]
    [PermitAuthorize("read", "audit_log")] // Requer ambas permissões
    public IActionResult BulkDelete([FromBody] string[] documentIds)
    {
        return Ok(new { message = $"Deleted {documentIds.Length} documents" });
    }
}

public record DocumentDto(string Title, string Content);