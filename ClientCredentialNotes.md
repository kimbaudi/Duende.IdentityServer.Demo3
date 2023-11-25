# Protecting an API using Client Credentials

Config.cs (IdentityServer)

```cs
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.InMemory;

public static class Config
{
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
                ClientName = "Client Credentials Client",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()) },

                AllowedScopes = { "weatherApiScope" }
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

install `Microsoft.AspNetCore.Authentication.JwtBearer` nuget package

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
    // options.TokenValidationParameters.ValidateAudience = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false
    };
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
