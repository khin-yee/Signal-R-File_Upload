using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using SIgnalRTest.Domain.IServices;

namespace SignalRTest.UI.Service
{
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
            if (_hubConnection == null)
                return;

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
            }
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

        public void ReceiveTwoMessageAsync<T1, T2,T3>(string methodName, Action<T1, T2,T3> handler)
        {
            _hubConnection.On(methodName, handler);
        }

        public async Task RegisterUserAsync(string username)
        {
            await _hubConnection.InvokeAsync("RegisterUser", username);
        }
        public async Task UnregisterUserAsync(string username)
        {
            await _hubConnection.InvokeAsync("UnregisterUser", username);
        }
        public void ListenUserListUpdated(Func<List<string>, Task> handler)
        {
            _hubConnection.On<List<string>>("UserListUpdated", handler);
        }

        public void ListenLastSeenUpdated(Func<Dictionary<string, string>, Task> handler)
        {
            _hubConnection.On<Dictionary<string, string>>("LastSeenUpdated", handler);
        }

        // ✅ Join a custom group room (reuses existing hub JoinGroup method)
        public async Task JoinCustomGroupAsync(string groupId)
        {
            await _hubConnection.InvokeAsync("JoinGroup", groupId);
        }

        // ✅ Listen for group messages (4 params: message, senderUserId, sendtime, groupId)
        public void ListenGroupMessage(Func<string, string, string, string, Task> handler)
        {
            _hubConnection.On<string, string, string, string>("ReceiveGroupMessage", handler);
        }

        // ✅ Listen for new group creation notification
        public void ListenGroupCreated(Func<string, string, string, List<string>, Task> handler)
        {
            _hubConnection.On<string, string, string, List<string>>("GroupCreated", handler);
        }

        public void ListenGroupUpdated(Func<string, string, List<string>, Task> handler)
        {
            _hubConnection.On<string, string, List<string>>("GroupUpdated", handler);
        }

        // ✅ NEW — remaining members receive updated member list after a removal
        public void ListenMemberRemoved(Func<string, string, List<string>, Task> handler)
        {
            _hubConnection.On<string, string, List<string>>("MemberRemoved", handler);
        }

        // ✅ NEW — the kicked user is told they were removed
        public void ListenRemovedFromGroup(Func<string, Task> handler)
        {
            _hubConnection.On<string>("RemovedFromGroup", handler);
        }

        // ✅ NEW — remaining members: someone left voluntarily
        public void ListenMemberLeft(Func<string, string, List<string>, Task> handler)
        {
            _hubConnection.On<string, string, List<string>>("MemberLeft", handler);
        }

        // ✅ NEW — the member who left receives this
        public void ListenLeftGroup(Func<string, Task> handler)
        {
            _hubConnection.On<string>("LeftGroup", handler);
        }

        // ✅ NEW — entire group deleted (last member left)
        public void ListenGroupDeleted(Func<string, Task> handler)
        {
            _hubConnection.On<string>("GroupDeleted", handler);
        }

        // ✅ NEW — admin role changed
        public void ListenAdminTransferred(Func<string, string, Task> handler)
        {
            _hubConnection.On<string, string>("AdminTransferred", handler);
        }
    }
}
