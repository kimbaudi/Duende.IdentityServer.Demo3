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

delete `Microsoft.EntityFrameworkCore.Sqlite` nuget package
and install `Microsoft.EntityFrameworkCore.SqlServer` nuget package.
also install `Duende.IdentityServer.EntityFramework` nuget package.

```shell
Uninstall-Package Microsoft.EntityFrameworkCore.Sqlite
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 6.0.0
Install-Package Duende.IdentityServer.EntityFramework -Version 6.3.2
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
