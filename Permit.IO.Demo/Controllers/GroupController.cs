using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Permit.IO.Demo.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IPermitService _permitService;

    public GroupController(IPermitService permitService)
    {
        _permitService = permitService;
    }

    [HttpGet]
    [PermitAuthorize("read", "group")]
    public IActionResult GetGroups()
    {
        return Ok(new { groups = new[] { "Admin", "Director", "Employee" } });
    }

    [HttpPost]
    [PermitAuthorize("write", "group")]
    public IActionResult CreateGroup([FromBody] GroupDto group)
    {
        return Ok(new { message = "Group created", group.Name });
    }

    [HttpDelete("{id}")]
    [PermitAuthorize("delete", "group", UseRouteId = true)]
    public IActionResult DeleteGroup(string id)
    {
        return Ok(new { message = $"Group {id} deleted" });
    }
}

public record GroupDto(string Name, string Description);