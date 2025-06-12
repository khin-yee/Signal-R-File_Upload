using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRUI2.Services;
public class SignalRService
{
    private readonly HubConnection _hubConnection;
    private readonly IConfiguration _configuration;

    public SignalRService(NavigationManager navigation, IConfiguration configuration)
    {
        _configuration = configuration;
        _hubConnection = new HubConnectionBuilder()
       .WithUrl(navigation.ToAbsoluteUri($"{_configuration!["ApiURl:baseurl"]}/signalR")).Build();
    }

    public async Task StartAsync()
    {
        await _hubConnection.StartAsync();
    }

    public async Task StopAsync()
    {
        await _hubConnection.StopAsync();
    }

    public async Task JoinGroupAsync(string groupName)
    {
        await _hubConnection.InvokeAsync("JoinGroup", groupName);
    }

    public void ReceiveMessageAsync<T>(string methodName, Action<T> handler)
    {
        _hubConnection.On<T>(methodName, handler);
    }

    public void ReceiveTwoMessageAsync<T1, T2>(string methodName, Action<T1, T2> handler)
    {
        _hubConnection.On(methodName, handler);
    }

}

