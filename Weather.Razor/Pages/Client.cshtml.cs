using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Weather.Razor.Pages;

[Authorize]
public class ClientModel : PageModel
{
    public void OnGet()
    {
    }
}
