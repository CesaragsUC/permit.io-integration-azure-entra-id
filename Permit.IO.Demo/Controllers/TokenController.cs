using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Permit.IO.Demo.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    [HttpGet("show")]
    public async Task<IActionResult> ShowToken()
    {
        // Tenta pegar os tokens
        var idToken = await HttpContext.GetTokenAsync("id_token");
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

        if (string.IsNullOrEmpty(idToken))
        {
            return BadRequest(new
            {
                Error = "Tokens not found. Make sure SaveTokens = true is configured.",
                Message = "Add 'options.SaveTokens = true;' to your authentication configuration."
            });
        }

        return Ok(new
        {
            IdToken = idToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer"
        });
    }

    [HttpGet("decode")]
    public async Task<IActionResult> DecodeToken()
    {
        var idToken = await HttpContext.GetTokenAsync("id_token");

        if (string.IsNullOrEmpty(idToken))
        {
            return BadRequest("No ID token found");
        }

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(idToken);

        return Ok(new
        {
            RawToken = idToken,
            Header = token.Header,
            Payload = new
            {
                Issuer = token.Issuer,
                Audiences = token.Audiences,
                ValidFrom = token.ValidFrom,
                ValidTo = token.ValidTo,
                Claims = token.Claims.Select(c => new { c.Type, c.Value })
            }
        });
    }

    [HttpGet("claims")]
    public IActionResult GetClaims()
    {
        var claims = User.Claims.Select(c => new
        {
            Type = c.Type,
            Value = c.Value
        }).ToList();

        return Ok(new
        {
            UserName = User.Identity?.Name,
            IsAuthenticated = User.Identity?.IsAuthenticated,
            Claims = claims,
            // Claims específicas importantes
            Email = User.FindFirst("email")?.Value
                    ?? User.FindFirst("preferred_username")?.Value,
            Name = User.FindFirst("name")?.Value,
            ObjectId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                      ?? User.FindFirst("oid")?.Value,
            TenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                      ?? User.FindFirst("tid")?.Value,
            Roles = User.Claims
                .Where(c => c.Type == "roles" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Groups = User.Claims
                .Where(c => c.Type == "groups")
                .Select(c => c.Value)
                .ToList()
        });
    }
}