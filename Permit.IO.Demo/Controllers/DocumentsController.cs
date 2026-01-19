using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Permit.IO.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IPermitService _permitService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IPermitService permitService,
        ILogger<DocumentsController> logger)
    {
        _permitService = permitService;
        _logger = logger;
    }

    [HttpGet]
    [PermitAuthorize("read", "document")]
    public IActionResult GetDocuments()
    {
        var userKey = _permitService.CreateUserKeyFromClaims(User);

        _logger.LogInformation("User {Email} listing documents", userKey.email);

        return Ok(new
        {
            message = "List of documents",
            user = new
            {
                email = userKey.email,
                firstName = userKey.firstName,
                lastName = userKey.lastName
            },
            documents = new[]
            {
                new { Id = 1, Title = "Document 1" },
                new { Id = 2, Title = "Document 2" }
            }
        });
    }

    [HttpPost]
    [PermitAuthorize("create", "document")]
    public IActionResult CreateDocument([FromBody] DocumentDto document)
    {
        var userKey = _permitService.CreateUserKeyFromClaims(User);

        return CreatedAtAction(
            nameof(GetDocument),
            new { id = 1 },
            new
            {
                message = "Document created",
                document,
                createdBy = userKey.email
            }
        );
    }

    [HttpGet("{id}")]
    [PermitAuthorize("read", "document", UseRouteId = true)]
    public IActionResult GetDocument(int id)
    {
        return Ok(new { id, title = $"Document {id}" });
    }

    [HttpDelete("{id}")]
    [PermitAuthorize("delete", "document", UseRouteId = true)]
    public IActionResult DeleteDocument(int id)
    {
        var userKey = _permitService.CreateUserKeyFromClaims(User);

        return Ok(new
        {
            message = $"Document {id} deleted by {userKey.email}"
        });
    }

    // Endpoint para verificar manualmente (sem atributo)
    [HttpGet("check-permission")]
    public async Task<IActionResult> CheckPermission()
    {
        var userKey = _permitService.CreateUserKeyFromClaims(User);

        var canRead = await _permitService.CheckPermissionAsync(userKey, "read", "document");
        var canCreate = await _permitService.CheckPermissionAsync(userKey, "create", "document");
        var canDelete = await _permitService.CheckPermissionAsync(userKey, "delete", "document");

        return Ok(new
        {
            user = userKey.email,
            permissions = new
            {
                canRead,
                canCreate,
                canDelete
            }
        });
    }
}