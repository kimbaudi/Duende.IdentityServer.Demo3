using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Weather.Razor.Pages;

public class LoginModel : PageModel
{
    public async Task OnPostAsync(string? returlUrl = null)
    {
        var props = new AuthenticationProperties { RedirectUri = returlUrl };

        await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, props);
    }
}
