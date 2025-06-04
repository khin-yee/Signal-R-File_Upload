using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using SIgnalRTest.Domain.IServices;

namespace SignalRTest.UI.Service
{
    public class SignalRService
    {
        private readonly HubConnection _hubConnection;

        public SignalRService(NavigationManager navigation)
        {
            _hubConnection = new HubConnectionBuilder()
           .WithUrl(navigation.ToAbsoluteUri("https://localhost:7055" + "/signalR")).Build();
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
    }
}
