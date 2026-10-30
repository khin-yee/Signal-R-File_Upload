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

    private static readonly ConcurrentDictionary<string, DateTime> _lastSeen
       = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
    public SignalRHub (IHubContext<SignalRHub> singnalRhub)
    {
        _singnalRhub = singnalRhub;
    }
    public async  Task RegisterUser(string username)
    {
        _userConnections[username] = Context.ConnectionId;
        await BroadcastUserList();
    }

    public static IEnumerable<string> GetOnlineUsers() => _userConnections.Keys;
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
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var entry = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
        if (entry.Key != null)
        {
            _userConnections.TryRemove(entry.Key, out _);
            // ✅ Safety net: record last seen if UnregisterUser wasn't called
            _lastSeen[entry.Key] = DateTime.UtcNow;
            await BroadcastUserList();
            await BroadcastLastSeen();
        }
        await base.OnDisconnectedAsync(exception);
    }
    private async Task BroadcastUserList()
    {
        var users = _userConnections.Keys.ToList();
        await _singnalRhub.Clients.All.SendAsync("UserListUpdated", users);
    }

    private async Task BroadcastLastSeen()
    {
        var lastSeenData = _lastSeen.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToString("o")   // ISO 8601 — "2026-10-06T05:00:00.000Z"
        );
        await _singnalRhub.Clients.All.SendAsync("LastSeenUpdated", lastSeenData);
    }
    public static IDictionary<string, DateTime> GetLastSeenTimes() => _lastSeen;
    public async Task UnregisterUser(string username)
    {
        _userConnections.TryRemove(username, out _);

        // ✅ Record last seen time
        _lastSeen[username] = DateTime.UtcNow;

        await BroadcastUserList();

        // ✅ Broadcast last seen to all clients so they update immediately
        await BroadcastLastSeen();
    }

    public async Task SendGroupMessage(string groupId, string message, string userid)
    {
        await _singnalRhub.Clients.Group(groupId)
            .SendAsync("ReceiveGroupMessage", message, userid,
                       DateTime.Now.ToShortTimeString(), groupId);
    }

    public async Task NotifyGroupCreated(string groupId, string groupName,
    string createdBy, List<string> members)
    {
        foreach (var member in members)
        {
            if (_userConnections.TryGetValue(member, out var connId))
            {
                // Add online member to the SignalR group room immediately
                await _singnalRhub.Groups.AddToGroupAsync(connId, groupId);
                // Notify them of the new group
                await _singnalRhub.Clients.Client(connId)
                    .SendAsync("GroupCreated", groupId, groupName, createdBy, members);
            }
        }
    }

    public async Task NotifyGroupUpdated(string groupId, string groupName,List<string> allMembers, List<string> newMembers)
    {
        await _singnalRhub.Clients.Group(groupId)
            .SendAsync("GroupUpdated", groupId, groupName, allMembers);

        // For brand-new members: add to SignalR room + notify their sidebar
        foreach (var member in newMembers)
        {
            if (_userConnections.TryGetValue(member, out var connId))
            {
                await _singnalRhub.Groups.AddToGroupAsync(connId, groupId);
                await _singnalRhub.Clients.Client(connId)
                    .SendAsync("GroupCreated", groupId, groupName, "admin", allMembers);
            }
        }
    }

    // ✅ NEW — notify everyone when a member is removed
    public async Task NotifyMemberRemoved(
        string groupId, string removedMember, List<string> remainingMembers)
    {
        // Tell all remaining members the updated member list
        await _singnalRhub.Clients.Group(groupId)
            .SendAsync("MemberRemoved", groupId, removedMember, remainingMembers);

        // Tell the removed user personally so their sidebar drops the group
        if (_userConnections.TryGetValue(removedMember, out var removedConnId))
        {
            // Remove from SignalR group room first
            await _singnalRhub.Groups.RemoveFromGroupAsync(removedConnId, groupId);
            // Then notify them
            await _singnalRhub.Clients.Client(removedConnId)
                .SendAsync("RemovedFromGroup", groupId);
        }
    }

    // ✅ NEW — notify everyone when a member voluntarily leaves
    public async Task NotifyMemberLeft(
        string groupId,
        string memberName,
        List<string> remainingMembers,
        string? newAdminName,
        bool groupDeleted,
        List<string> allMemberConnections)   // pass original member list connection IDs
    {
        if (groupDeleted)
        {
            // Notify ALL former members (before removal) that group is gone
            foreach (var connId in allMemberConnections)
            {
                await _singnalRhub.Clients.Client(connId)
                    .SendAsync("GroupDeleted", groupId);
                await _singnalRhub.Groups.RemoveFromGroupAsync(connId, groupId);
            }
        }
        else
        {
            // Tell remaining members someone left
            await _singnalRhub.Clients.Group(groupId)
                .SendAsync("MemberLeft", groupId, memberName, remainingMembers);

            // If admin changed, tell everyone the new admin
            if (newAdminName != null)
            {
                await _singnalRhub.Clients.Group(groupId)
                    .SendAsync("AdminTransferred", groupId, newAdminName);
            }

            // Tell the leaver personally so their sidebar removes the group
            if (_userConnections.TryGetValue(memberName, out var leaverConnId))
            {
                await _singnalRhub.Groups.RemoveFromGroupAsync(leaverConnId, groupId);
                await _singnalRhub.Clients.Client(leaverConnId)
                    .SendAsync("LeftGroup", groupId);
            }
        }
    }

    public static bool TryGetConnection(string username, out string? connId)
        => _userConnections.TryGetValue(username, out connId);
}
