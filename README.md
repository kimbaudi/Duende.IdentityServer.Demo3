# Duende IdentityServer Demo 3 (Sql Server + ASP.NET Identity + EF Core)

## Setup

I'm using .NET Core 6.

list donet sdks and make sure version 6 is available.
in my case, i have 6.0.412 so I use that.

```shell
dotnet --list-sdks
# 6.0.412 [C:\Program Files\dotnet\sdk]
# 7.0.403 [C:\Program Files\dotnet\sdk]
```

create a new global.json file in the directory where you will create the solution.
in my case, I will create the solution under D:\dev\dotnet folder
so I'll add the global.json folder in the dotnet folder

```shell
# create a new global.json file
dotnet new globaljson
```

edit global.json to target the version 6 sdk.
you should now have a global.json folder in D:\dev\dotnet folder

```json
{
  "sdk": {
    "version": "6.0.412"
  }
}
```

open the Command Prompt and make sure you are under D:\dev\dotnet directory.
uninstall and install/reinistall the duende identityserver template.
run `dotnet new --list` to ensure that the template is installed.

```shell
# uninstall
dotnet new --uninstall Duende.IdentityServer.Templates

# install
dotnet new --install Duende.IdentityServer.Templates

# list templates
dotnet new --list
```

first, create an empty solution project called `Duende.IdentityServer.Demo3`.
then cd into that directory.

```shell
cd /d/dev/dotnet
dotnet new sln -o Duende.IdentityServer.Demo3
cd Duende.IdentityServer.Demo3
```

create a new project using the asp.net core identity template.
I called mine `Duende.IdentityServer.SqlServer`

```shell
cd /d/dev/dotnet/Duende.IdentityServer.Demo3
dotnet new isaspid -o Duende.IdentityServer.SqlServer
```

open the solution in Visual Studio by double-clicking the `Duende.IdentityServer.Demo3.sln` file.
add existing project `Duende.IdentityServer.SqlServer` to the solution.

uninstall & reinstall existing nuget packages.
also install `Duende.IdentityServer.EntityFramework` nuget package.
also replace `Microsoft.EntityFrameworkCore.Sqlite` with `Microsoft.EntityFrameworkCore.SqlServer`

```shell
Uninstall-Package Duende.IdentityServer.AspNetIdentity
Uninstall-Package Duende.IdentityServer.EntityFramework
Uninstall-Package Microsoft.AspNetCore.Authentication.Google
Uninstall-Package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
Uninstall-Package Microsoft.AspNetCore.Identity.EntityFrameworkCore
Uninstall-Package Microsoft.AspNetCore.Identity.UI
Uninstall-Package Microsoft.EntityFrameworkCore.Sqlite
Uninstall-Package Microsoft.EntityFrameworkCore.SqlServer
Uninstall-Package Microsoft.EntityFrameworkCore.Tools
Uninstall-Package Serilog.AspNetCore

Install-Package Duende.IdentityServer.AspNetIdentity -Version 6.3.6
Install-Package Duende.IdentityServer.EntityFramework -Version 6.3.6
Install-Package Microsoft.AspNetCore.Authentication.Google -Version 6.0.25
Install-Package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore -Version 6.0.25
Install-Package Microsoft.AspNetCore.Identity.EntityFrameworkCore -Version 6.0.25
Install-Package Microsoft.AspNetCore.Identity.UI -Version 6.0.25
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 6.0.25
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 6.0.25
Install-Package Serilog.AspNetCore -Version 6.1.0
```

edit `HostingExtensions.cs` file.
create variables `connectionString` & `migrationsAssembly` and place it above the line containing `builder.Services.AddRazorPages();`.
update `AddDbContext` to use sql server w/ connectionString var.
comment out or delete the `.AddInMemory` lines.
add configuration store & operational store.

```cs
// begin add1
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
// end add1

builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // update
    options.UseSqlServer(connectionString, o => o.MigrationsAssembly(migrationsAssembly));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
    options.EmitStaticAudienceClaim = true;
})
// begin add2
.AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
})
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(migrationsAssembly));
})
// end add2
// comment out or remove
// .AddInMemoryIdentityResources(Config.IdentityResources)
// .AddInMemoryApiScopes(Config.ApiScopes)
// .AddInMemoryClients(Config.Clients)
.AddAspNetIdentity<ApplicationUser>();
```

edit ConnectionStrings in `appsettings.json`

```json
{
  // before
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AspIdUsers.db;"
  },
  // after
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AspIdUsers;Trusted_Connection=True"
  }
}
```

use Package Manager Console in Visual Studio to add migrations for the configuration store & operational store.

```shell
Get-Migration -Context ApplicationDbContext
Remove-Migration -Context ApplicationDbContext
Drop-Database -Context ApplicationDbContext

Add-Migration InitialIdentityServerMigration -Context ApplicationDbContext -OutputDir Data/Migrations/ApplicationDb
Add-Migration InitialIdentityServerMigration -Context ConfigurationDbContext -OutputDir Data/Migrations/ConfigurationDb
Add-Migration InitialIdentityServerMigration -Context PersistedGrantDbContext -OutputDir Data/Migrations/PersistedGrantDb

Update-Database -Context ApplicationDbContext
Update-Database -Context ConfigurationDbContext
Update-Database -Context PersistedGrantDbContext

Get-Migration -Context ApplicationDbContext
Get-Migration -Context ConfigurationDbContext
Get-Migration -Context PersistedGrantDbContext

# Remove-Migration -Context ApplicationDbContext
# Remove-Migration -Context ConfigurationDbContext
# Remove-Migration -Context PersistedGrantDbContext
```

you can also delete `buildschema.bat` & `buildschema.sh` files

now we need to seed the database.

first, update `Config.cs` file to the following.
we are basically creating an api scope called `weatherApiScope`, then setting that as the allowed scope in our clients.
we are also creating an api resource called `weatherApiResource` and setting the scope as `weatherApiScope`.

Config.cs

```cs
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.SqlServer;

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
            new("weatherApiScope"),
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

            // interactive client using code flow + pkce
            new() {
                ClientId = "interactive",
                ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44300/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile, "weatherApiScope" }
            },
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            new("weatherApiResource")
            {
                Scopes = { "weatherApiScope" },
                ApiSecrets = { new Secret("ScopeSecret".Sha256()) },
            }
        };
}
```

next, update `SeedData.cs` to the following so it seeds the config & users.

SeedData.cs

```cs
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.SqlServer.Data;
using Duende.IdentityServer.SqlServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;

namespace Duende.IdentityServer.SqlServer;

public class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        scope.ServiceProvider.GetService<ApplicationDbContext>().Database.Migrate();
        scope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.Migrate();

        var context = scope.ServiceProvider.GetService<ConfigurationDbContext>();
        context.Database.Migrate();
        EnsureSeedData(context);
        EnsureUsers(scope);
    }

    private static void EnsureSeedData(ConfigurationDbContext context)
    {
        if (!context.Clients.Any())
        {
            Log.Debug("Clients being populated");
            foreach (var client in Config.Clients.ToList())
            {
                context.Clients.Add(client.ToEntity());
            }
            context.SaveChanges();
        }
        else
        {
            Log.Debug("Clients already populated");
        }

        if (!context.IdentityResources.Any())
        {
            Log.Debug("IdentityResources being populated");
            foreach (var resource in Config.IdentityResources.ToList())
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
            context.SaveChanges();
        }
        else
        {
            Log.Debug("IdentityResources already populated");
        }

        if (!context.ApiScopes.Any())
        {
            Log.Debug("ApiScopes being populated");
            foreach (var resource in Config.ApiScopes.ToList())
            {
                context.ApiScopes.Add(resource.ToEntity());
            }
            context.SaveChanges();
        }
        else
        {
            Log.Debug("ApiScopes already populated");
        }

        if (!context.ApiResources.Any())
        {
            Log.Debug("ApiResources being populated");
            foreach (var resource in Config.ApiResources.ToList())
            {
                context.ApiResources.Add(resource.ToEntity());
            }
            context.SaveChanges();
        }
        else
        {
            Log.Debug("ApiResources already populated");
        }

        if (!context.IdentityProviders.Any())
        {
            Log.Debug("OIDC IdentityProviders being populated");
            context.IdentityProviders.Add(new OidcProvider
            {
                Scheme = "demoidsrv",
                DisplayName = "IdentityServer",
                Authority = "https://demo.duendesoftware.com",
                ClientId = "login",
            }.ToEntity());
            context.SaveChanges();
        }
        else
        {
            Log.Debug("OIDC IdentityProviders already populated");
        }
    }

    private static void EnsureUsers(IServiceScope scope)
    {
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var alice = userMgr.FindByNameAsync("alice").Result;
        if (alice == null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
            };
            var result = userMgr.CreateAsync(alice, "Pass123$").Result;
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = userMgr.AddClaimsAsync(alice, new Claim[]{
                            new Claim(JwtClaimTypes.Name, "Alice Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Alice"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                        }).Result;
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }
            Log.Debug("alice created");
        }
        else
        {
            Log.Debug("alice already exists");
        }

        var bob = userMgr.FindByNameAsync("bob").Result;
        if (bob == null)
        {
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true
            };
            var result = userMgr.CreateAsync(bob, "Pass123$").Result;
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = userMgr.AddClaimsAsync(bob, new Claim[]{
                            new Claim(JwtClaimTypes.Name, "Bob Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Bob"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                            new Claim("location", "somewhere")
                        }).Result;
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }
            Log.Debug("bob created");
        }
        else
        {
            Log.Debug("bob already exists");
        }
    }
}
```

Now seed the database by right-clicking on the `Duende.IdentityServer.Sqlite` project and selecting Properties.
Select Debug on the left menu and under General, select Open debug launch profiles UI link.
Enter /seed for the Command line arguments.

Once the database is seeded, make sure to remove the command line argument.

## Demo 1

let's see if we can get an access token.

run `Duende.IdentityServer.SqlServer`.

execute the following command in a shell using curl to get an access token

```shell
curl -X POST -H "content-type: application/x-www-form-urlencoded" -H "Cache-Control: no-cache" -d "client_id=m2m.client&scope=weatherApiScope&client_secret=511536EF-F270-4058-80CA-1C89C192F69A&grant_type=client_credentials" "https://localhost:5001/connect/token"
```

or use Postman to get access token

![Postman get access token](img/postman-get-access-token.png)

## Demo 2

Let's add a webapi project so we can protect the api w/ a bearer token from Duende Identity Server.

Right-click solution and add new webapi project called `Weather.API`

open `LaunchSettings.json` file and take note of the port number. In my case, the port is 7232 for https. You can change it, but make sure to keep note of it.

Open `WeatherForecastController.cs` and add the `[Authorize]` attribute to the controller.

Next, install `Microsoft.AspNetCore.Authentication.JwtBearer`.

```shell
# install using pmc
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 6.0.25

# uninstall using pmc
Uninstall-Package Microsoft.AspNetCore.Authentication.JwtBearer
```

Next, edit `Program.cs` in Weather.API and configure authentication

Program.cs

```cs
builder.Services.AddSwaggerGen();

// begin add1
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //the url of IdentityServer
        options.Authority = "https://localhost:5001";
        // name of the audience
        options.Audience = "weatherApiResource";

        options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
    });
// end add1

var app = builder.Build();

// ...

app.UseHttpsRedirection();

// begin add2
app.UseAuthentication();
// end add2
app.UseAuthorization();

app.MapControllers();
```

Now right-click on the solution and configure both `Duende.IdentityServer.SqlServer` and `Weather.API` as startup projects. Run the projects afterwards.

Use curl or Postman to get the weatherforecast

```shell
# this won't work
curl -X GET -H "Cache-Control: no-cache" "https://localhost:7232/weatherforecast"
```

```shell
# get the token
curl -X POST -H "content-type: application/x-www-form-urlencoded" -H "Cache-Control: no-cache" -d "client_id=m2m.client&scope=weatherApiScope&client_secret=511536EF-F270-4058-80CA-1C89C192F69A&grant_type=client_credentials" "https://localhost:5001/connect/token"

# replace <ADDTOKENHERE> w/ the access token
curl -X GET -H "Authorization: Bearer <ADDTOKENHERE>" -H "Cache-Control: no-cache" "https://localhost:7232/weatherforecast"

# example here
curl -X GET -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkQyNzY4NkVDOTJEOEZCQkZGNjYxREFFRjY4NkUwOTMzIiwidHlwIjoiYXQrand0In0.eyJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo1MDAxIiwibmJmIjoxNzAwNTg5MzY1LCJpYXQiOjE3MDA1ODkzNjUsImV4cCI6MTcwMDU5Mjk2NSwiYXVkIjpbIndlYXRoZXJhcGkiLCJodHRwczovL2xvY2FsaG9zdDo1MDAxL3Jlc291cmNlcyJdLCJzY29wZSI6WyJzY29wZTEiXSwiY2xpZW50X2lkIjoibTJtLmNsaWVudCIsImp0aSI6IjQ3QjdDNzFBOUIyRERCQ0Q4NURGNkI1NTlEN0I2RTVDIn0.A_FVKb-VCNWvZm60dXZOyEPWtmB4UfZj-_C2RdjtYTNPzUgQkFte4NZ53kvnEe3sRCWAESoHzFYxOpewDpywFbOUontcY1dZEbJH-NxY16B8ofNrNgR7YHuVx28OXJinoGNohxr-Z_OVniQoHL09sBPsXy8lyN4B_esMuXtZiykRf-8p51gwHZZZhwGYsxv4yCnm06f5ac4DqJjIhIu6QcFCGBV4KdYv3baZUNIiXzgzSu3wh6K9QysNWmgPEETbG1shTMbzmzer1IZf8HvoxkEJzlnrt87HEvAIlZvaluLBlISsrVdxgfj1nNkHn0-qgmjfe_-Ya4V6lm5tkBhhhQ" -H "Cache-Control: no-cache" "https://localhost:7232/weatherforecast"
```

## Demo 3

Now let's create a Razor Page web application called `Weather.Razor` that accesses the protected Weather.API endpoint by requesting a token from Identity Server.

In my case, `Weather.Razor` web app is running on port 7157.

Open `Program.cs` and add the following code:

```cs
var builder = WebApplication.CreateBuilder(args);

// add this
builder.Services.AddHttpClient();
```

Add `Models` folder to `Weather.Razor` and create `WeatherData.cs`

```cs
namespace Weather.Razor.Models;

public class WeatherData
{
    public DateTime Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; set; }
}
```

Add a new empty razor page called `Weather.cshtml`

Open `Weather.cshtml.cs` and add the following:

```cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Weather.Razor.Models;

namespace Weather.Razor.Pages
{
    public class WeatherModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherModel> _logger;

        public WeatherModel(IHttpClientFactory httpClientFactory, ILogger<WeatherModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public IEnumerable<WeatherData>? WeatherData { get; set; }

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var httpResponseMessage = await httpClient.GetAsync("https://localhost:7232/weatherforecast");
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                WeatherData = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
    }
}
```

Open `Weather.cshtml` and add the following:

```cs
@page
@model WeatherModel
@{
    ViewData["Title"] = "Weather Page";
}

<h2>Weather</h2>

@if (Model.WeatherData != null)
{
    <table>
        @foreach (var item in Model.WeatherData)
        {
            <tr>
                <td>@item.Date</td>
                <td>@item.Summary</td>
                <td>@item.TemperatureC</td>
                <td>@item.TemperatureF</td>
            </tr>
        }
    </table>
}
```

To test that the Razor Page webapp is able to hit the API endpoint, open `WeatherForecastController.cs` in the `Weather.API` project and comment out `[Authorize]` attribute.

Configure startup projects to include `Weather.Razor`.
Start the applications and go to <https://localhost:7157/weather>.
You should see the Weather table.

Now let's uncomment `[Authorize]` attribute in `WeatherForecastController.cs` file.
If we try to access <https://localhost:7157/weather>, we will no longer see the weather table.
Let's now fix that.

First, install `IdentityModel` nuget package.

```shell
# install using pmc
Install-Package IdentityModel -Version 6.2.0

# uninstall using pmc
Uninstall-Package IdentityModel
```

In `Weather.Razor` web application, add a new folder `Services`.
Then add files `IdentityServerSettings.cs`, `ITokenService.cs` and `TokenService.cs`.

IdentityServerSettings.cs

```cs
namespace Weather.Razor.Services;

public class IdentityServerSettings
{
    public string DiscoveryUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientPassword { get; set; } = string.Empty;
    public bool UseHttps { get; set; }
}
```

ITokenService.cs

```cs
using IdentityModel.Client;

namespace Weather.Razor.Services;

public interface ITokenService
{
    Task<TokenResponse> GetTokenAsync(string scope);
}
```

TokenService.cs

```cs
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Weather.Razor.Services;

public class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private readonly IOptions<IdentityServerSettings> _identityServerSettings;
    private readonly DiscoveryDocumentResponse _discoveryDocument;

    public TokenService(ILogger<TokenService> logger, IOptions<IdentityServerSettings> identityServerSettings)
    {
        _logger = logger;
        _identityServerSettings = identityServerSettings;

        using var httpClient = new HttpClient();
        _discoveryDocument = httpClient.GetDiscoveryDocumentAsync(identityServerSettings.Value.DiscoveryUrl).Result;
        if (_discoveryDocument.IsError)
        {
            _logger.LogError("Unable to get discovery document. Error is {Error}", _discoveryDocument.Error);
            throw new Exception("Unable to get discovery document", _discoveryDocument.Exception);
        }
    }

    public async Task<TokenResponse> GetTokenAsync(string scope)
    {
        using var client = new HttpClient();
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = _discoveryDocument.TokenEndpoint,

            ClientId = _identityServerSettings.Value.ClientId,
            ClientSecret = _identityServerSettings.Value.ClientPassword,
            Scope = scope
        });

        if (tokenResponse.IsError)
        {
            _logger.LogError("Unable to get token. Error is {Error}", tokenResponse.Error);
            throw new Exception("Unable to get token", tokenResponse.Exception);
        }

        return tokenResponse;
    }
}
```

Open `appsettings.json` (or better yet, use user secrets `secrets.json`)
and add the following configuration

```json
{
  "IdentityServerSettings": {
    "DiscoveryUrl": "https://localhost:5001",
    "ClientId": "m2m.client",
    "ClientPassword": "511536EF-F270-4058-80CA-1C89C192F69A",
    "UseHttps": true
  },
}
```

Open `Program.cs` and add the following:

```cs
builder.Services.AddHttpClient();
// begin add
builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection(nameof(IdentityServerSettings)));
builder.Services.AddSingleton<ITokenService, TokenService>();
// end add
```

Open `Weather.cshtml.cs` and update `OnGetAsync` method:

```cs
public async Task OnGetAsync()
{
    var httpClient = _httpClientFactory.CreateClient();

    // begin add
    var token = await _tokenService.GetTokenAsync("weatherApiScope");
    if (token.AccessToken != null)
    {
        httpClient.SetBearerToken(token.AccessToken);
    }
    // end add

    var httpResponseMessage = await httpClient.GetAsync("https://localhost:7232/weatherforecast");
    if (httpResponseMessage.IsSuccessStatusCode)
    {
        using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        WeatherData = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
```

So now we are able to access the protected weather api endpoint from our weather razor page web application.
However, the Razor Page web application is not protected.

To protect `Weather.Razor` web application, add `[Authorize]` attribute to the `WeatherModel` class in `Weather.cshtml.cs` file.

Then open `appsettings.json` file in the razor page app and add the following configuration

```json
{
  "InteractiveServiceSettings": {
    "AuthorityUrl": "https://localhost:5001",
    "ClientId": "interactive",
    "ClientSecret": "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0",
    "Scopes": ["weatherApiScope"]
  },
}
```

Open `Config.cs` file in `Duende.IdentityServer.SqlServer` and edit the ports for the interactive client to match the port of the Razor Page web app (port 7157). Also, make sure `weatherApiScope` is included in the AllowedScopes:

Config.cs

```cs
// interactive client using code flow + pkce
new Client
{
    ClientId = "interactive",
    ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },
        
    AllowedGrantTypes = GrantTypes.Code,

    // change ports to 7157 (so it matches the port of the MVC web app)
    RedirectUris = { "https://localhost:7157/signin-oidc" },
    FrontChannelLogoutUri = "https://localhost:7157/signout-oidc",
    PostLogoutRedirectUris = { "https://localhost:7157/signout-callback-oidc" },

    AllowOfflineAccess = true,
    AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile, "weatherApiScope" }
},
```

Also, update the port to 7157 in the sqlite database.

In the `Weather.Razor` application, install the `Microsoft.AspNetCore.Authentication.OpenIdConnect` nuget package

```shell
Install-Package Microsoft.AspNetCore.Authentication.OpenIdConnect -Version 6.0.25

Uninstall-Package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

Open `Program.cs` file configure authentication

Program.cs

```cs
builder.Services.AddHttpClient();
builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection("IdentityServerSettings"));
builder.Services.AddSingleton<ITokenService, TokenService>();

// begin add
builder.Services
    .AddAuthentication(options =>
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
        options.Scope.Add(builder.Configuration["InteractiveServiceSettings:Scopes:0"]);

        options.ResponseType = "code";
        options.UsePkce = true;
        options.ResponseMode = "query";
        options.SaveTokens = true;
    });
// end add


app.UseRouting();

// begin add
app.UseAuthentication();
// end add
app.UseAuthorization();
```

So now that we have client authentication setup, the client will be able to access certain scopes.
And as part of this authentication, it will get back an access token.

Now that we are getting back an access token as part of our user authentication, we no longer need to use the TokenService to get the token.

So update the `OnGetAsync` method in `Weather.cshtml.cs` to get the token from the http context:

```cs
public async Task OnGetAsync()
{
    var httpClient = _httpClientFactory.CreateClient();

    // begin add
    var token = await HttpContext.GetTokenAsync("access_token");
    if (token != null)
    {
        httpClient.SetBearerToken(token);
    }
    // end add

    var httpResponseMessage = await httpClient.GetAsync("https://localhost:7232/weatherforecast");
    if (httpResponseMessage.IsSuccessStatusCode)
    {
        using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        WeatherData = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
```

Since we no longer get the access token from TokenService, we can delete the following file:

- IdentityServerSettings.cs
- ITokenService.cs
- TokenService.cs

And also remove IdentityServerSettings configuration from appsettings.json (or secrets.json)
as well as remove references to those files in Program.cs and Weather.cshtml.cs.
