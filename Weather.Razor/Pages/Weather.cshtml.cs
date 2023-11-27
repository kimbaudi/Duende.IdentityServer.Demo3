using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Weather.Razor.Models;

namespace Weather.Razor.Pages;

[Authorize]
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

        var token = await HttpContext.GetTokenAsync("access_token");
        if (token != null)
        {
            httpClient.SetBearerToken(token);
        }

        var httpResponseMessage = await httpClient.GetAsync("https://localhost:7232/weatherforecast");
        if (httpResponseMessage.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
            WeatherData = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
