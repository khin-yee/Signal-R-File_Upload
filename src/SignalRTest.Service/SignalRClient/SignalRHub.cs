using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignalRTest.Service.SignalRClient;
public class SignalRHub : Hub
{
    private readonly IHubContext<SignalRHub> _singnalRhub;

    private static readonly ConcurrentDictionary<string, string> _userConnections
       = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public SignalRHub (IHubContext<SignalRHub> singnalRhub)
    {
        _singnalRhub = singnalRhub;
    }
    public Task RegisterUser(string username)
    {
        _userConnections[username] = Context.ConnectionId;
        return Task.CompletedTask;
    }
    public async Task SendToUser(string recipientUsername, string method, string message, string senderUserId)
    {
        if (_userConnections.TryGetValue(recipientUsername, out var connectionId))
        {
            // Found the connection — send only to them
            await _singnalRhub.Clients.Client(connectionId)
                .SendAsync(method, message, senderUserId, DateTime.Now.ToShortTimeString());
        }
        // If user not found / offline — silently skip (or throw if you prefer)
    }
    public async Task SendSignalR(string groupId, string method,string message,string userid)
    {
        await _singnalRhub.Clients.Group(groupId).SendAsync(method, message,userid,DateTime.Now.ToShortTimeString());
    }

    public async Task SendAll(string method, string message)
    {
        await _singnalRhub.Clients.All.SendAsync(method, message);
    }
    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // Find the entry where value matches this connection and remove it
        var entry = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
        if (entry.Key != null)
        {
            _userConnections.TryRemove(entry.Key, out _);
        }
        return base.OnDisconnectedAsync(exception);
    }
}
