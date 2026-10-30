using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalRTest.Service.SignalRClient;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Models;
using SIgnalRTest.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignalRTest.Service;
public class SignalRService:ISignalRService
{
    private readonly IConfiguration _configuration;

    private readonly IGroupRepository _groupRepository;

    public readonly SignalRHub _signalR;

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IMessageRepository _messageRepository;
    public SignalRService (SignalRHub signalR,IConfiguration configuration, IHttpClientFactory httpClientFactory, IMessageRepository messageRepo, IGroupRepository groupRepository)
    {
        _signalR = signalR;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _messageRepository = messageRepo;
        _groupRepository = groupRepository;
    }
    public ApiResponse MessageCreate(string groupId)
    {
        return new ApiResponse();
    }
    public async Task SendMessage(string groupId, string message, string userid, string sendMode, string? recipientUserId)
    {
        await _messageRepository.SaveAsync(new ChatMessage
        {
            SenderId      = userid,
            RecipientId   = sendMode == "Direct" ? recipientUserId : null,
            SendMode      = sendMode,
            GroupId       = groupId,
            Message       = message,
            SentAt        = DateTime.UtcNow,
            SentAtDisplay = DateTime.Now.ToShortTimeString()
        });
        if (sendMode == "Direct" && !string.IsNullOrEmpty(recipientUserId))
        {
            await _signalR.SendToUser(recipientUserId, "ReceiveMessage", message, userid);
        }
        else if (sendMode == "Group" && !string.IsNullOrEmpty(groupId))
            await _signalR.SendGroupMessage(groupId, message, userid);
        else
        {
            await _signalR.SendSignalR("123", "ReceiveMessage", message, userid);
        }
    }
    private async Task<string> GetManagementApiToken()
    {
        var domain = _configuration["Auth0:Domain"];
        var clientId = _configuration["Auth0:ClientId"];
        var secret = _configuration["Auth0:ClientSecret"];
        var audience = _configuration["Auth0:Audience"];
        var client = _httpClientFactory.CreateClient();
        var payload = new
        {
            grant_type = "client_credentials",
            client_id = clientId,
            client_secret = secret,
            audience = audience
        };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://{domain}/oauth/token", content);
        var json = await response.Content.ReadAsStringAsync();
        var token = JObject.Parse(json)["access_token"]?.ToString();
        return token ?? throw new Exception("Failed to obtain Auth0 management token.");
    }
    public async Task<List<Auth0UserResponse>> GetAuth0Users(string? searchQuery = null)
    {
        var domain = _configuration["Auth0:Domain"];
        var token = await GetManagementApiToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        string url;
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var encoded = Uri.EscapeDataString(searchQuery);
            url = $"https://{domain}/api/v2/users?q=email:*{encoded}*+OR+name:*{encoded}*&search_engine=v3&per_page=50";
        }
        else
        {
            url = $"https://{domain}/api/v2/users?per_page=50&include_totals=false";
        }
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<Auth0UserResponse>>(json)
               ?? new List<Auth0UserResponse>();
    }
    public async Task<Auth0UserResponse?> ValidateAuth0User(string usernameOrEmail)
    {
        var users = await GetAuth0Users(usernameOrEmail);
        return users?.FirstOrDefault();
    }
    public async Task<List<ChatMessage>> GetMessages(string currentUserId, string sendMode,string? contactId = null, string groupId = "123",DateTime? after = null)
    {
        if (sendMode == "All" || sendMode == "Group")
            return await _messageRepository.GetGroupMessagesAsync(groupId, after);
        if (!string.IsNullOrEmpty(contactId))
            return await _messageRepository.GetDirectMessagesAsync(
                currentUserId, contactId, after);
        return new List<ChatMessage>();
    }

    public async Task<ChatGroup> CreateGroup(string groupName, string createdBy,List<string> members)
    {
        // Always include the creator in the group
        if (!members.Contains(createdBy, StringComparer.OrdinalIgnoreCase))
            members.Insert(0, createdBy);

        var group = new ChatGroup
        {
            GroupName = groupName,
            CreatedBy = createdBy,
            Members   = members,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _groupRepository.CreateAsync(group);

        // Notify all ONLINE members via SignalR
        await _signalR.NotifyGroupCreated(
            created.Id!, groupName, createdBy, members);

        return created;
    }

    public async Task<List<ChatGroup>> GetGroups(string username)
    {
        return await _groupRepository.GetByMemberAsync(username);
    }

    public async Task<ChatGroup?> UpdateGroup(string groupId,string requestedBy,string? newName,List<string> membersToAdd)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group ==null) return null;
        if (!group.CreatedBy.Equals(requestedBy, StringComparison.OrdinalIgnoreCase)) return null;
        if (!string.IsNullOrWhiteSpace(newName)&& !newName.Equals(group.GroupName, StringComparison.OrdinalIgnoreCase))
        {
            await _groupRepository.UpdateNameAsync(groupId, newName);
        }
        if (membersToAdd.Any())
        {
            group = await _groupRepository.AddMembersAsync(groupId, membersToAdd)??group;
        }
        var updated = await _groupRepository.GetByIdAsync(groupId)??group;
        await _signalR.NotifyGroupUpdated(groupId, updated.GroupName, updated.Members, membersToAdd);

        return updated;
    }

    // ✅ NEW — admin removes one member from the group
    public async Task<ChatGroup?> RemoveMember(string groupId, string requestedBy, string memberToRemove)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null) return null;

        // Only admin can remove members
        if (!group.CreatedBy.Equals(requestedBy, StringComparison.OrdinalIgnoreCase))
            return null;

        // Admin cannot remove themselves
        if (memberToRemove.Equals(requestedBy, StringComparison.OrdinalIgnoreCase))
            return null;

        var updated = await _groupRepository.RemoveMemberAsync(groupId, memberToRemove);
        if (updated == null) return null;

        // Notify all remaining members + the removed user via SignalR
        await _signalR.NotifyMemberRemoved(groupId, memberToRemove, updated.Members);

        return updated;
    }
    // ✅ NEW
    public async Task LeaveGroup(string groupId, string memberName)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null) return;

        // Validate the member is actually in this group
        if (!group.Members.Contains(memberName, StringComparer.OrdinalIgnoreCase))
            return;

        // Capture original member connection IDs BEFORE removal (needed for GroupDeleted path)
        var originalMemberConnIds = group.Members
            .Select(m => { SignalRHub.TryGetConnection(m, out var c); return c; })
            .Where(c => c != null)
            .ToList()!;

        // Remove member from MongoDB
        var updated = await _groupRepository.LeaveGroupAsync(groupId, memberName);

        // Case 1: No members left → delete the group
        if (updated == null || !updated.Members.Any())
        {
            await _groupRepository.DeleteAsync(groupId);
            await _signalR.NotifyMemberLeft(
                groupId, memberName,
                remainingMembers: new List<string>(),
                newAdminName: null,
                groupDeleted: true,
                allMemberConnections: originalMemberConnIds!);
            return;
        }

        // Case 2: Admin left but members remain → transfer admin
        string? newAdmin = null;
        if (group.CreatedBy.Equals(memberName, StringComparison.OrdinalIgnoreCase))
        {
            newAdmin = updated.Members.First();
            await _groupRepository.TransferAdminAsync(groupId, newAdmin);
        }

        // Case 3 (and Case 2 continued): Notify everyone
        await _signalR.NotifyMemberLeft(
            groupId, memberName,
            remainingMembers: updated.Members,
            newAdminName: newAdmin,
            groupDeleted: false,
            allMemberConnections: originalMemberConnIds!);
    }
}

