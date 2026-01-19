using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Permit.IO.Demo;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options => {
    options.Filters.Add<PermitAuthorizeFilter>();
});

builder.Services.AddOpenApi();

builder.Services.AddScoped<IPermitService, PermitService>();
builder.Services.AddScoped<PermitAuthorizeFilter>();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Permit.io Demo API with Azure Entra ID",
        Version = "v1",
        Description = "API com autenticação Azure AD e autorização Permit.io"
    });

    var tenantId = builder.Configuration["AzureAd:TenantId"];
    var apiClientId = builder.Configuration["AzureAd:ClientId"];
    var scope = $"api://{apiClientId}/access_as_user";
    var scopeList = builder.Configuration.GetSection("AzureAd:Scopes:Permissions")?.Get<string[]>();

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Name = "oauth2",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { $"api://{apiClientId}/access_as_user", "Access API as user" },
                    { $"api://{apiClientId}/App.Read", "Read access to API" },
                    { $"api://{apiClientId}/App.Write", "Write access to API" },
                    { $"api://{apiClientId}/App.Delete", "Delete access to API" }
                }
            }
        }
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
       // [new OpenApiSecuritySchemeReference("oauth2", document)] = [scope]
        [new OpenApiSecuritySchemeReference("oauth2", document)] = scopeList?.ToList() ?? new List<string>()
    });
});

// Configurar autenticação (exemplo com JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
.EnableTokenAcquisitionToCallDownstreamApi()
.AddInMemoryTokenCaches();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Permit.io Demo API v1");

        var swaggerClientId = builder.Configuration["SwaggerClient:ClientId"]; // SPA client
        options.OAuthClientId(swaggerClientId);
        options.OAuthUsePkce();
        options.OAuthScopeSeparator(" ");
    });


    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// IMPORTANTE: Ordem correta dos middlewares
app.UseAuthentication();  // 1º - Identifica o usuário
//app.UsePermitAuthorization();  // 2º - Verifica permissões Permit.io
app.UseAuthorization();    // 3º - Autorização padrão .NET (se necessário)

app.MapControllers();

app.Run();