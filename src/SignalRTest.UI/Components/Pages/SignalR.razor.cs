using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using SignalRTest.UI.Service;
using SIgnalRTest.Domain.Models;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;

using System.Text.RegularExpressions;

namespace SignalRTest.UI.Components.Pages;
public partial class SignalR : ComponentBase, IDisposable
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string Name { get; set; }
    public string? currentmessage { get; set; }
    public string? author { get; set; } = "other";
    public string? message { get; set; }
    public MessageRequest? messageRequest { get; set; } = new MessageRequest();
    public List<MessageRequest> messages { get; set; } = new List<MessageRequest>();

    public string SendMode { get; set; } = "All";
    public string? RecipientUserId { get; set; }
    public bool IsDirectMode => SendMode == "Direct";

    public List<ConversationItem> Conversations { get; set; } = new();
    public ConversationItem? ActiveConversation { get; set; }
    public Dictionary<string, List<MessageRequest>> ConversationMessages { get; set; } = new();
    public List<MessageRequest> ActiveMessages =>
        ActiveConversation != null &&
        ConversationMessages.TryGetValue(ActiveConversation.ContactName, out var msgs)
            ? msgs
            : new List<MessageRequest>();
    public string SearchQuery { get; set; } = "";
    public bool IsSearching { get; set; } = false;
    public List<Auth0UserResponse> SearchResults { get; set; } = new();
    public string? SearchError { get; set; }
    public List<string> OnlineUsers { get; set; } = new();
    // ── Logout injections ──
    private System.Threading.Timer? _lastSeenTimer;

    // ── Custom Group state ──────────────────────────────────────
    public bool ShowCreateGroupDialog { get; set; } = false;
    public string NewGroupName { get; set; } = "";
    public HashSet<string> SelectedGroupMembers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    public List<Auth0UserResponse> AllUsersForGroupSelect { get; set; } = new();
    public string? ActiveCustomGroupId { get; set; }   // for the open custom group conv
    public bool IsCreatingGroup { get; set; } = false;
    public string? CreateGroupError { get; set; }

    // ── Group Settings state ────────────────────────────────────
    public bool ShowGroupSettings { get; set; } = false;
    public string EditGroupName { get; set; } = "";
    public HashSet<string> MembersToAdd { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    public bool IsUpdatingGroup { get; set; } = false;
    public string? UpdateGroupError { get; set; }
    public string? UpdateGroupSuccess { get; set; }

    public bool IsGroupAdmin =>
        ActiveConversation?.CustomGroupId != null &&
        Name.Equals(ActiveConversation.CreatedBy, StringComparison.OrdinalIgnoreCase);

    [Inject]
    public UtilitiesService? _service { get; set; }

    [Inject] public IJSRuntime JS { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public required SignalRService signalRService { get; set; }

    public void Dispose()
    {
        _lastSeenTimer?.Dispose();
    }
    protected override async Task OnInitializedAsync()
    {
        await signalRService.StartAsync();
        await signalRService.JoinGroupAsync("123");
        ListenSignalREvent();
        ListenUserListEvent();
        ListenGroupMessageEvent();   // ✅ NEW
        ListenGroupCreatedEvent();   // ✅ NEW
        ListenLastSeenEvent();
        ListenGroupUpdatedEvent();   // ✅ NEW

        await signalRService.RegisterUserAsync(Name);

        var groupConv = new ConversationItem
        {
            ContactName     = "Group Chat",
            IsGroup         = true,
            IsOnline        = true,
            LastMessage     = "Send a message to everyone",
            AvatarColor     = "#ede9fe",
            AvatarTextColor = "#4f46e5"
        };
        Conversations.Add(groupConv);

        // Set active conversation BEFORE loading history
        // so LoadConversationHistory knows not to count group messages as unread
        ActiveConversation = groupConv;
        SendMode = "All";

        // Load group chat history (cache-first → MongoDB sync)
        // ✅ Bug fix: removed the line that was overwriting this with an empty list
        await LoadConversationHistory(groupConv);

        _lastSeenTimer = new System.Threading.Timer(async _ =>
        {
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        await LoadAllUsers();
        await LoadUserGroups();          // ✅ NEW — load user's custom groups

        _ = CheckAllUnreadCounts();

        await base.OnInitializedAsync();
    }
    private async Task LoadUserGroups()
    {
        var response = await _service!.GetGroups(Name);
        if (response.ErrorCode != "00" || string.IsNullOrEmpty(response.Detail)) return;

        var groups = JsonConvert.DeserializeObject<List<ChatGroup>>(response.Detail);
        if (groups == null) return;

        foreach (var group in groups)
        {
            // Join the SignalR room so we receive messages
            await signalRService.JoinCustomGroupAsync(group.Id!);

            // Add to conversation list if not already there
            if (!Conversations.Any(c => c.CustomGroupId == group.Id))
            {
                var conv = BuildGroupConvItem(group);
                Conversations.Add(conv);
                ConversationMessages[conv.ContactName] = new List<MessageRequest>();
            }
        }

        await InvokeAsync(StateHasChanged);
    }
    private void ListenGroupUpdatedEvent()
    {
        signalRService.ListenGroupUpdated(async (groupId, newName, allMembers) =>
        {
            var conv = Conversations.FirstOrDefault(c => c.CustomGroupId == groupId);
            if (conv == null) return;

            conv.ContactName = newName;
            conv.Members     = allMembers;

            // Keep settings panel in sync if it's open
            if (ActiveConversation?.CustomGroupId == groupId)
                EditGroupName = newName;

            await InvokeAsync(StateHasChanged);
        });
    }
    private ConversationItem BuildGroupConvItem(ChatGroup group)
    {
        return new ConversationItem
        {
            ContactName     = group.GroupName,
            IsGroup         = true,
            CustomGroupId   = group.Id,
            Members         = group.Members,
            IsOnline        = true,
            LastMessage     = $"Created by {group.CreatedBy}",
            AvatarColor     = "#dbeafe",
            AvatarTextColor = "#1e40af",
            CreatedBy       = group.CreatedBy
        };
    }

    private string GetCacheKey(ConversationItem conv)
    {
        if (conv.CustomGroupId != null)
            return $"chat_customgroup_{conv.CustomGroupId}";   // ✅ NEW
        if (conv.IsGroup)
            return "chat_group_123";
        var names = new[] { Name, conv.ContactName }.OrderBy(n => n).ToArray();
        return $"chat_direct_{names[0]}|{names[1]}";
    }

    // Read cached messages from browser localStorage
    private async Task<List<MessageRequest>> ReadFromCache(string key)
    {
        try
        {
            var json = await JS.InvokeAsync<string?>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(json)) return new List<MessageRequest>();
            return JsonConvert.DeserializeObject<List<MessageRequest>>(json)
                   ?? new List<MessageRequest>();
        }
        catch { return new List<MessageRequest>(); }
    }

    // Write messages to browser localStorage (keep last 100 only)
    private async Task WriteToCache(string key, List<MessageRequest> messages)
    {
        try
        {
            // Keep only last 100 messages to stay well within 5MB localStorage limit
            var trimmed = messages.Count > 100
                ? messages.Skip(messages.Count - 100).ToList()
                : messages;
            var json = JsonConvert.SerializeObject(trimmed);
            await JS.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch { /* localStorage might be disabled — fail silently */ }
    }

    // Read last sync timestamp from localStorage
    private async Task<DateTime?> ReadLastSync(string syncKey)
    {
        try
        {
            var val = await JS.InvokeAsync<string?>("localStorage.getItem", syncKey);
            if (string.IsNullOrEmpty(val)) return null;
            if (DateTime.TryParse(val, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return null;
        }
        catch { return null; }
    }

    // Save last sync timestamp
    private async Task WriteLastSync(string syncKey, DateTime syncTime)
    {
        try
        {
            await JS.InvokeVoidAsync("localStorage.setItem", syncKey, syncTime.ToString("o"));
        }
        catch { }
    }

    private string GetSyncKey(ConversationItem conv) => $"sync_{GetCacheKey(conv)}";

    private async Task LoadConversationHistory(ConversationItem conv)
    {
        var cacheKey = GetCacheKey(conv);  
        var syncKey  = GetSyncKey(conv) ;  

        // Step 1: render from localStorage immediately
        var cached = await ReadFromCache(cacheKey);
        if (cached.Count > 0)
        {
            ConversationMessages[conv.ContactName] = cached;
            await InvokeAsync(StateHasChanged);
        }

        // Step 2: incremental sync from MongoDB
        var lastSync = await ReadLastSync(syncKey);

        // ✅ Determine sendMode and groupId for API call
        string apiSendMode;
        string? apiContactId = null;
        string apiGroupId = "123";

        if (conv.CustomGroupId != null)
        {
            apiSendMode = "Group";
            apiGroupId  = conv.CustomGroupId;
        }
        else if (conv.IsGroup)
        {
            apiSendMode = "All";
            apiGroupId  = "123";
        }
        else
        {
            apiSendMode  = "Direct";
            apiContactId = conv.ContactName;
        }

        var response = await _service!.GetMessages(Name, apiSendMode, apiContactId, apiGroupId, lastSync);

        if (response.ErrorCode == "00" && !string.IsNullOrEmpty(response.Detail))
        {
            var newMessages = JsonConvert.DeserializeObject<List<ChatMessage>>(response.Detail)
                              ?? new List<ChatMessage>();

            if (newMessages.Count > 0)
            {
                var mapped = newMessages.Select(m => new MessageRequest
                {
                    message         = m.Message,
                    userid          = m.SenderId == Name ? "You" : m.SenderId,
                    sendtime        = m.SentAtDisplay,
                    sendmode        = m.SendMode,
                    recipientUserid = m.RecipientId,
                    groupId         = m.GroupId
                }).ToList();

                var merged = cached.Concat(mapped).ToList();
                ConversationMessages[conv.ContactName] = merged;

                var last = merged.LastOrDefault();
                if (last != null)
                {
                    conv.LastMessage     = last.message ?? "";
                    conv.LastMessageTime = last.sendtime ?? "";
                }

                if (ActiveConversation?.ContactName != conv.ContactName)
                {
                    var newUnread = mapped.Count(m => m.userid != "You");
                    if (newUnread > 0) conv.UnreadCount += newUnread;
                }

                await WriteToCache(cacheKey, merged);
                await InvokeAsync(StateHasChanged);
            }
        }

        await WriteLastSync(syncKey, DateTime.UtcNow);
    }
    private async Task CheckAllUnreadCounts()
    {
        foreach (var conv in Conversations.Where(c => c != ActiveConversation).ToList())
        {
            await LoadConversationHistory(conv);
        }
    }

    public async void SelectConversation(ConversationItem conv)
    {
        ActiveConversation = conv;
        conv.UnreadCount   = 0;
        SearchResults.Clear();
        SearchQuery = "";

        if (conv.CustomGroupId != null)
        {
            // ✅ Custom group
            SendMode          = "Group";
            ActiveCustomGroupId = conv.CustomGroupId;
            RecipientUserId   = null;
        }
        else if (conv.IsGroup)
        {
            // Global "Group Chat"
            SendMode          = "All";
            ActiveCustomGroupId = null;
            RecipientUserId   = null;
        }
        else
        {
            // Direct
            SendMode          = "Direct";
            ActiveCustomGroupId = null;
            RecipientUserId   = conv.ContactName;
        }

        StateHasChanged();
        await LoadConversationHistory(conv);
    }
    private void ListenSignalREvent()
    {
        signalRService.ReceiveTwoMessageAsync<string, string,string>("ReceiveMessage", (msg, userid,sendtime) =>
        {
            if (userid == Name) return;
            var incoming = new MessageRequest
            {
                message  = msg,
                userid   = userid,
                sendtime = sendtime,
                sendmode = "All"
            };
            // Always add to Group Chat
            if (!ConversationMessages.ContainsKey("Group Chat"))
                ConversationMessages["Group Chat"] = new();
            ConversationMessages["Group Chat"].Add(incoming);
            // Update Group Chat preview
            var groupConv = Conversations.FirstOrDefault(c => c.IsGroup);
            if (groupConv != null)
            {
                groupConv.LastMessage     = $"{userid}: {msg}";
                groupConv.LastMessageTime = sendtime;
                if (ActiveConversation?.ContactName != "Group Chat")
                    groupConv.UnreadCount++;
            }
            // Also add to the sender's direct conversation
            var senderConv = Conversations.FirstOrDefault(c =>
                c.ContactName.Equals(userid, StringComparison.OrdinalIgnoreCase));
            if (senderConv == null)
            {
                // New contact — add them to the list
                senderConv = new ConversationItem
                {
                    ContactName     = Name,
                    IsGroup         = false,
                    IsOnline        = true,
                    AvatarColor     = GetAvatarColor(userid),
                    AvatarTextColor = GetAvatarTextColor(userid)
                };
                Conversations.Insert(1, senderConv);
                ConversationMessages[userid] = new();
            }
            ConversationMessages[userid].Add(new MessageRequest
            {
                message  = msg,
                userid   = userid,
                sendtime = sendtime,
                sendmode = "Direct"
            });
            senderConv.LastMessage     = msg;
            senderConv.LastMessageTime = sendtime;
            var cacheKey = GetCacheKey(senderConv);
             WriteToCache(cacheKey, ConversationMessages[userid]);
            if (ActiveConversation?.ContactName != userid)
                senderConv.UnreadCount++;
            // Toast only when not in that chat
            if (ActiveConversation?.ContactName != userid &&
                ActiveConversation?.ContactName != "Group Chat")
            {
                Snackbar.Add($"New message from {userid}", Severity.Info);
            }
            InvokeAsync(StateHasChanged);
        });
    }
    private void ListenLastSeenEvent()
    {
        signalRService.ListenLastSeenUpdated(async lastSeenData =>
        {
            foreach (var kvp in lastSeenData)
            {
                // Match by display name or email prefix
                var conv = Conversations.FirstOrDefault(c =>
                    !c.IsGroup && (
                        c.ContactName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        c.ContactEmail.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Split('@')[0].Equals(c.ContactName, StringComparison.OrdinalIgnoreCase)
                    ));
                if (conv != null && DateTime.TryParse(kvp.Value, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var lastSeenUtc))
                {
                    conv.LastSeen = lastSeenUtc;
                }
            }
            await InvokeAsync(StateHasChanged);
        });
    }
    public string GetLastSeenText(ConversationItem conv)
    {
        if (conv.IsGroup) return "All members";
        if (conv.IsOnline) return "Online";
        if (conv.LastSeen == null) return "Offline";
        var diff = DateTime.UtcNow - conv.LastSeen.Value;
        if (diff.TotalMinutes < 1) return "Active just now";
        if (diff.TotalMinutes < 60) return $"Active {(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"Active {(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"Active {(int)diff.TotalDays}d ago";
        return $"Last seen {conv.LastSeen.Value.ToLocalTime():MMM d}";
    }
    private bool IsUserOnline(string contactName, string contactEmail = "")
    {
        return OnlineUsers.Any(u =>
            u.Equals(contactName, StringComparison.OrdinalIgnoreCase) ||
            u.Equals(contactEmail, StringComparison.OrdinalIgnoreCase) ||
            u.Split('@')[0].Equals(contactName, StringComparison.OrdinalIgnoreCase));
    }
   

    public async Task SearchUsers()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            SearchError = null;
            return;
        }
        IsSearching = true;
        SearchError = null;
        SearchResults.Clear();
        await InvokeAsync(StateHasChanged);
        var response = await _service!.GetUsers(SearchQuery);
        if (response.ErrorCode == "00")
        {
            var all = JsonConvert.DeserializeObject<List<Auth0UserResponse>>(response.Detail ?? "[]") ?? new();
            // Filter out self
            SearchResults = all
                .Where(u => !u.DisplayName.Equals(Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!SearchResults.Any())
                SearchError = "No users found.";
        }
        else
        {
            SearchError = "Search failed. Please try again.";
        }
        IsSearching = false;
        await InvokeAsync(StateHasChanged);
    }
    public async Task HandleSearchKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await SearchUsers();
    }

    public void StartDirectChat(Auth0UserResponse user)
    {
        var name = user.DisplayName;
        var existing = Conversations.FirstOrDefault(c =>
            c.ContactName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            existing = new ConversationItem
            {
                ContactName     = name,
                ContactEmail    = user.Email,
                IsGroup         = false,
                IsOnline = IsUserOnline(name, user.Email),
                AvatarColor     = GetAvatarColor(name),
                AvatarTextColor = GetAvatarTextColor(name)
            };
            Conversations.Insert(1, existing);
            ConversationMessages[name] = new List<MessageRequest>();
        }
        SearchQuery = "";
        SearchResults.Clear();
        SelectConversation(existing);
    }

    public async Task<ApiResponse> CallApi()
    {
        if (string.IsNullOrWhiteSpace(message) || ActiveConversation == null)
            return new ApiResponse();

        // Validate recipient for direct messages
        if (!ActiveConversation.IsGroup)
        {
            var validation = await _service!.ValidateUser(ActiveConversation.ContactName);
            if (validation.ErrorCode != "00")
            {
                Snackbar.Add(
                    $"User '{ActiveConversation.ContactName}' was not found in Auth0.",
                    Severity.Error);
                return new ApiResponse { ErrorCode = "04", ErrorMessage = "User not found" };
            }
        }

        var response = await _service!.CallApi(message!,Name,SendMode,IsDirectMode ? RecipientUserId : null,ActiveConversation.CustomGroupId );

        var sentMsg = new MessageRequest
        {
            message         = message,
            sendtime        = DateTime.Now.ToShortTimeString(),
            userid          = "You",
            recipientUserid = IsDirectMode ? RecipientUserId : null,
            sendmode        = SendMode,
            groupId         = ActiveConversation.CustomGroupId
        };

        var key = ActiveConversation.ContactName;
        if (!ConversationMessages.ContainsKey(key))
            ConversationMessages[key] = new();
        ConversationMessages[key].Add(sentMsg);

        ActiveConversation.LastMessage     = message;
        ActiveConversation.LastMessageTime = sentMsg.sendtime;
        message = "";

        await WriteToCache(GetCacheKey(ActiveConversation), ConversationMessages[key]);
        await InvokeAsync(StateHasChanged);
        return response;
    }
    public async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(message))
            await CallApi();
    }

    private static readonly (string bg, string text)[] AvatarPalette =
    {
        ("#fce7f3", "#be185d"), // pink
        ("#d1fae5", "#065f46"), // green
        ("#fef3c7", "#92400e"), // amber
        ("#dbeafe", "#1e40af"), // blue
        ("#f3e8ff", "#6b21a8"), // purple
        ("#ffedd5", "#9a3412"), // orange
    };
    private string GetAvatarColor(string name)
    {
        var idx = Math.Abs(name.GetHashCode()) % AvatarPalette.Length;
        return AvatarPalette[idx].bg;
    }
    private string GetAvatarTextColor(string name)
    {
        var idx = Math.Abs(name.GetHashCode()) % AvatarPalette.Length;
        return AvatarPalette[idx].text;
    }

    private async Task LoadAllUsers()
    {
        var response = await _service!.GetUsers();
        if (response.ErrorCode == "00" && !string.IsNullOrEmpty(response.Detail))
        {
            var users = JsonConvert.DeserializeObject<List<Auth0UserResponse>>(response.Detail);
            if (users != null)
            {
                foreach (var user in users)
                {
                    var displayName = user.Name;
                    if (displayName.Equals(Name, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!Conversations.Any(c => c.ContactName.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Conversations.Add(new ConversationItem
                        {
                            ContactName     = displayName,
                            ContactEmail    = user.Email,
                            IsGroup         = false,
                            IsOnline        = OnlineUsers.Contains(displayName, StringComparer.OrdinalIgnoreCase),
                            AvatarColor     = GetAvatarColor(displayName),
                            AvatarTextColor = GetAvatarTextColor(displayName)
                        });
                        ConversationMessages[displayName] = new List<MessageRequest>();
                    }
                }
                AllUsersForGroupSelect = users
    .Where(u => !u.DisplayName.Equals(Name, StringComparison.OrdinalIgnoreCase))
    .ToList();
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private void ListenUserListEvent()
    {
        signalRService.ListenUserListUpdated(async onlineUserNames =>
        {
            OnlineUsers = onlineUserNames;
            foreach (var conv in Conversations.Where(c => !c.IsGroup))
                conv.IsOnline = IsUserOnline(conv.ContactName, conv.ContactEmail);
            await InvokeAsync(StateHasChanged); // ✅ properly awaited
        });
    }

    public async Task ConfirmLogout()
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Sign out",
            "Are you sure you want to sign out?",
            yesText: "Sign out",
            cancelText: "Cancel"
        );

        if (confirmed == true)
        {
            await signalRService.UnregisterUserAsync(Name);
            await signalRService.StopAsync();
            if (AuthenticationStateProvider is CustomAuthStateProvider p)
                p.MarkUserAsLoggedOut();

            Navigation.NavigateTo("/", forceLoad: true);
        }
    }

    private void ListenGroupMessageEvent()
    {
        signalRService.ListenGroupMessage(async (msg, userid, sendtime, groupId) =>
        {
            if (userid == Name) return;

            // Find the custom group conversation
            var conv = Conversations.FirstOrDefault(c => c.CustomGroupId == groupId);
            if (conv == null) return;

            var incoming = new MessageRequest
            {
                message  = msg,
                userid   = userid,
                sendtime = sendtime,
                sendmode = "Group",
                groupId  = groupId
            };

            if (!ConversationMessages.ContainsKey(conv.ContactName))
                ConversationMessages[conv.ContactName] = new();
            ConversationMessages[conv.ContactName].Add(incoming);

            conv.LastMessage     = $"{userid}: {msg}";
            conv.LastMessageTime = sendtime;

            if (ActiveConversation?.CustomGroupId != groupId)
                conv.UnreadCount++;

            // Update localStorage cache
            await WriteToCache(GetCacheKey(conv), ConversationMessages[conv.ContactName]);

            await InvokeAsync(StateHasChanged);
        });
    }

    private void ListenGroupCreatedEvent()
    {
        signalRService.ListenGroupCreated(async (groupId, groupName, createdBy, members) =>
        {
            // Avoid duplicates (creator already added it locally)
            if (Conversations.Any(c => c.CustomGroupId == groupId)) return;

            // Join the SignalR group room
            await signalRService.JoinCustomGroupAsync(groupId);

            var group = new ChatGroup
            {
                Id        = groupId,
                GroupName = groupName,
                CreatedBy = createdBy,
                Members   = members
            };
            var conv = BuildGroupConvItem(group);
            Conversations.Add(conv);
            ConversationMessages[conv.ContactName] = new List<MessageRequest>();

            // Show notification
            Snackbar.Add($"You were added to group \"{groupName}\"", Severity.Info);

            await InvokeAsync(StateHasChanged);
        });
    }

    public async Task CreateGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName))
        {
            CreateGroupError = "Please enter a group name.";
            return;
        }
        if (!SelectedGroupMembers.Any())
        {
            CreateGroupError = "Please select at least one member.";
            return;
        }

        IsCreatingGroup = true;
        CreateGroupError = null;
        await InvokeAsync(StateHasChanged);

        var members = SelectedGroupMembers.ToList();
        var response = await _service!.CreateGroup(NewGroupName, Name, members);

        if (response.ErrorCode == "00" && !string.IsNullOrEmpty(response.Detail))
        {
            var created = JsonConvert.DeserializeObject<ChatGroup>(response.Detail);
            if (created != null)
            {
                // Join the SignalR room (creator needs to join too)
                await signalRService.JoinCustomGroupAsync(created.Id!);

                var conv = BuildGroupConvItem(created);
                Conversations.Add(conv);
                ConversationMessages[conv.ContactName] = new List<MessageRequest>();

                // Close dialog and reset
                ShowCreateGroupDialog = false;
                NewGroupName          = "";
                SelectedGroupMembers.Clear();

                // Switch to the new group
                SelectConversation(conv);
            }
        }
        else
        {
            CreateGroupError = "Failed to create group. Please try again.";
        }

        IsCreatingGroup = false;
        await InvokeAsync(StateHasChanged);
    }

    public void ToggleMember(string username)
    {
        if (!SelectedGroupMembers.Remove(username))
            SelectedGroupMembers.Add(username);
        StateHasChanged();
    }

    public void OpenGroupSettings()
    {
        if (ActiveConversation?.CustomGroupId == null) return;
        EditGroupName      = ActiveConversation.ContactName;
        MembersToAdd.Clear();
        UpdateGroupError   = null;
        UpdateGroupSuccess = null;
        ShowGroupSettings  = true;
        StateHasChanged();
    }

    public async Task SaveGroupChanges()
    {
        if (ActiveConversation?.CustomGroupId == null) return;

        var nameChanged = !string.IsNullOrWhiteSpace(EditGroupName) &&
            !EditGroupName.Equals(ActiveConversation.ContactName,
                StringComparison.OrdinalIgnoreCase);

        if (!nameChanged && !MembersToAdd.Any())
        {
            UpdateGroupError = "No changes to save.";
            return;
        }

        IsUpdatingGroup    = true;
        UpdateGroupError   = null;
        UpdateGroupSuccess = null;
        await InvokeAsync(StateHasChanged);

        var response = await _service!.UpdateGroup(
            ActiveConversation.CustomGroupId,
            Name,
            nameChanged ? EditGroupName : null,
            MembersToAdd.ToList()
        );

        if (response.ErrorCode == "00" && !string.IsNullOrEmpty(response.Detail))
        {
            var updated = JsonConvert.DeserializeObject<ChatGroup>(response.Detail);
            if (updated != null)
            {
                ActiveConversation.ContactName = updated.GroupName;
                ActiveConversation.Members     = updated.Members;
                MembersToAdd.Clear();
                UpdateGroupSuccess = "Group updated successfully!";
            }
        }
        else if (response.ErrorCode == "03")
        {
            UpdateGroupError = "Only the group admin can make changes.";
        }
        else
        {
            UpdateGroupError = "Failed to update group. Please try again.";
        }

        IsUpdatingGroup = false;
        await InvokeAsync(StateHasChanged);
    }

    public void ToggleAddMember(string username)
    {
        if (!MembersToAdd.Remove(username))
            MembersToAdd.Add(username);
        StateHasChanged();
    }
}

