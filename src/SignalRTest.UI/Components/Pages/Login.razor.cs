using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using SignalRTest.UI.Service;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
namespace SignalRTest.UI.Components.Pages;
public partial class Login : ComponentBase
{
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject] public IApiCallService _apiService { get; set; }

    private string Username;

    private string Password;

    private string Error;

    private async Task HandleLogin()
    {
        var payload = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", Username },
            { "password", Password },
            { "client_id", "kcOHYxPyPY5Q1ZAV1J9IdNQ8acymQLOz" },
            { "client_secret", "Uw6Xb8mCd61mmyazpKID6os0YzG3PW8gt3y_Q9JOqrzozebRJ6QnDe2D-v6hGKxk" }
        };

        var url = "https://dev-885urtcfxbrkfg3b.us.auth0.com/oauth/token";

        var apirequest = new ApiRequest(HttpMethod.Post, url, payload, "aa");

        var response = await _apiService.APICall(apirequest);

        if (response.ErrorCode == "00")
        {
            var result = JsonConvert.DeserializeObject<Auth0TokenResponse>(response.Detail!);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
 
            var jwt = handler.ReadJwtToken(result.id_token);

            var claims = jwt.Claims;

            // Mark user as authenticated in your provider
            if (AuthenticationStateProvider is CustomAuthStateProvider customAuthStateProvider)
            {
                customAuthStateProvider.MarkUserAsAuthenticated(claims.FirstOrDefault(c => c.Type == "name")?.Value ?? Username,claims);
            }
            var name = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? Username;
            Navigation.NavigateTo($"/signalRTest?name={Uri.EscapeDataString(name)}");
        }
        else
        {
            Error = "Invalid login";
        }
    }
}

