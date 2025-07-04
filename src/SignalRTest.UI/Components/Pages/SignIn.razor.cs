using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SignalRTest.UI.Service;
namespace SignalRTest.UI.Components.Pages;
public partial class SignIn : ComponentBase
{
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    private string Username;
    private string Password;
    public string Email;
    private string Error;

    private async Task HandleLogin()
    {
      
    }


    public class Auth0TokenResponse
    {
        public string access_token { get; set; }
        public string id_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }
}

