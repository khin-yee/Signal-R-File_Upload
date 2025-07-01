using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SignalRTest.UI.Service;
namespace SignalRTest.UI.Components.Pages;
public partial class Login : ComponentBase
{
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    private string Username;
    private string Password;
    private string Error;

    private async Task HandleLogin()
    {
        Error = string.Empty;
        var payload = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", Username },
            { "password", Password },
            { "client_id", "kcOHYxPyPY5Q1ZAV1J9IdNQ8acymQLOz" },
            { "client_secret", "Uw6Xb8mCd61mmyazpKID6os0YzG3PW8gt3y_Q9JOqrzozebRJ6QnDe2D-v6hGKxk" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://dev-885urtcfxbrkfg3b.us.auth0.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(payload)
        };

        var response = await Http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Auth0TokenResponse>();

            // Parse the ID token to get claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.id_token);

            var claims = jwt.Claims;

            // Mark user as authenticated in your provider
            if (AuthenticationStateProvider is CustomAuthStateProvider customAuthStateProvider)
            {
                customAuthStateProvider.MarkUserAsAuthenticated(
                    claims.FirstOrDefault(c => c.Type == "email")?.Value ?? Username,
                    claims);
            }
            var email = claims.FirstOrDefault(c => c.Type == "email")?.Value ?? Username;
            Navigation.NavigateTo($"/signalRTest?email={Uri.EscapeDataString(email)}");
        }
        else
        {
            Error = "Invalid login";
        }
    }


    public class Auth0TokenResponse
    {
        public string access_token { get; set; }
        public string id_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }
}

