# Interactive Applications with ASP.NET Core

Config.cs (IdentityServer)

```cs
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.InMemory;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new("weatherApiScope", "Weather API Scope"),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // m2m client credentials flow client
            new() {
                ClientId = "m2m.client",
                ClientSecrets = { new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()) },

                AllowedGrantTypes = GrantTypes.ClientCredentials,

                AllowedScopes = { "weatherApiScope" }
            },

            // interactive client using code flow + pkce
            new() {
                ClientId = "interactive",
                ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:7267/signin-oidc" },
                PostLogoutRedirectUris = { "https://localhost:7267/signout-callback-oidc" },

                AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile }
            },
        };
}
```

use Package Manager Console in Visual Studio to remove existing migrations and add a new one w/ the modified configuration in `Config.cs`:

```shell
Get-Migration -Context ApplicationDbContext
Remove-Migration -Context ApplicationDbContext
Drop-Database -Context ApplicationDbContext

Get-Migration -Context ConfigurationDbContext
Remove-Migration -Context ConfigurationDbContext
Drop-Database -Context ConfigurationDbContext

Get-Migration -Context PersistedGrantDbContext
Remove-Migration -Context PersistedGrantDbContext
Drop-Database -Context PersistedGrantDbContext

Add-Migration InitialIdentityServerMigration -Context ApplicationDbContext -OutputDir Data/Migrations/ApplicationDb
Add-Migration InitialIdentityServerMigration -Context ConfigurationDbContext -OutputDir Data/Migrations/ConfigurationDb
Add-Migration InitialIdentityServerMigration -Context PersistedGrantDbContext -OutputDir Data/Migrations/PersistedGrantDb

Update-Database -Context ApplicationDbContext
Update-Database -Context ConfigurationDbContext
Update-Database -Context PersistedGrantDbContext

Get-Migration -Context ApplicationDbContext
Get-Migration -Context ConfigurationDbContext
Get-Migration -Context PersistedGrantDbContext
```

In Weather.API, install `Microsoft.AspNetCore.Authentication.JwtBearer` nuget package

```shell
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 6.0.25
# Uninstall-Package Microsoft.AspNetCore.Authentication.JwtBearer
```

Program.cs (Weather.API)

```cs
// ...
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Authority = "https://localhost:5001";
    options.TokenValidationParameters.ValidateAudience = false;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "weatherApiScope");
    });
});
// ...

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization("ApiScope");
```

WeatherController.cs (Weather.API)

add `[Authorize]` attribute

```cs
[ApiController]
[Route("[controller]")]
[Authorize]
public class WeatherForecastController : ControllerBase
{
    // ...
}
```

In Weather.MVC, install `Microsoft.AspNetCore.Authentication.OpenIdConnect` nuget package

```shell
Install-Package Microsoft.AspNetCore.Authentication.OpenIdConnect -Version 6.0.25
# Uninstall-Package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

appsettings.json (Weather.MVC)

```json
{
  "InteractiveServiceSettings": {
    "AuthorityUrl": "https://localhost:5001",
    "ClientId": "interactive",
    "ClientSecret": "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0"
  }
}
```

Program.cs (Weather.MVC)

```cs
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["InteractiveServiceSettings:AuthorityUrl"];

    options.ClientId = builder.Configuration["InteractiveServiceSettings:ClientId"];
    options.ClientSecret = builder.Configuration["InteractiveServiceSettings:ClientSecret"];
    options.ResponseType = "code";

    options.SaveTokens = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute().RequireAuthorization();

app.Run();
```
