using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Newtonsoft.Json;
using SignalRTest.UI.Service;
using SIgnalRTest.Domain.Models;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace SignalRTest.UI.Components.Pages;
public partial class SignIn : ComponentBase
{
    [Inject] 
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private string Username;

    private string Password;

    public string Email;

    private string Error;

    [Inject]
    public IApiCallService? _apiservice { get; set; }

    private async Task CreateAccount()
    {
        //HttpMethod method, string url, object? requestBody = default!, string? token = default!
        string token =await  GetManagementTokenAsync();
        string url = "https://dev-885urtcfxbrkfg3b.us.auth0.com/api/v2/users";
        var newUser = new
        {
            name = Username,
            email = Email,
            password = Password,
            username = Username,
            connection = "Username-Password-Authentication"
        };

        var apiRequest = new ApiRequest(HttpMethod.Post, url,newUser,token);

        var response = await _apiservice!.APICall(apiRequest);


        if (!string.IsNullOrEmpty(response.Detail))
        {
            Snackbar.Add(response!.Detail, Severity.Error);

            Navigation.NavigateTo("/SignIn");
            
        }
        else
        {
            Snackbar.Add("SignUp Successful! You can logIn", Severity.Success);

            Navigation.NavigateTo("/");
        }
    }

    public async Task<string> GetManagementTokenAsync()
    {
        var client = new HttpClient();
        var requestBody = GetRequestBody();

        var request = new HttpRequestMessage(HttpMethod.Post, "https://dev-885urtcfxbrkfg3b.us.auth0.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(requestBody)
        };

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get token: {json}");

        var obj = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
        return obj.GetProperty("access_token").GetString();
    }

    private static Dictionary<string, string> GetRequestBody()
    {
        return new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", "kcOHYxPyPY5Q1ZAV1J9IdNQ8acymQLOz" },
            { "client_secret", "Uw6Xb8mCd61mmyazpKID6os0YzG3PW8gt3y_Q9JOqrzozebRJ6QnDe2D-v6hGKxk" },
            { "audience", "https://dev-885urtcfxbrkfg3b.us.auth0.com/api/v2/" }

        };
    }
}

