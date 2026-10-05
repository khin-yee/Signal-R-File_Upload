using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Newtonsoft.Json;
using SignalRTest.UI.Service;
using SIgnalRTest.Domain.Models;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using System.Text.RegularExpressions;

namespace SignalRTest.UI.Components.Pages;
public partial class SignalR : ComponentBase
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


    [Inject]
    public UtilitiesService? _service { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public required SignalRService signalRService { get; set; }
    protected override async Task OnInitializedAsync()
    {
        await signalRService.StartAsync();
        await signalRService.JoinGroupAsync("123");
        await signalRService.RegisterUserAsync(Name);

        var groupConv = new ConversationItem
        {
            ContactName    = "Group Chat",
            IsGroup        = true,
            IsOnline       = true,
            LastMessage    = "Send a message to everyone",
            AvatarColor    = "#ede9fe",
            AvatarTextColor = "#4f46e5"
        };
        Conversations.Add(groupConv);
        ConversationMessages["Group Chat"] = new List<MessageRequest>();
        //SelectConversation(groupConv); // default open = Group Chat
        ListenSignalREvent();
        ListenUserListEvent();
        await LoadAllUsers();
        await base.OnInitializedAsync();
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
                    ContactName     = userid,
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

    public void SelectConversation(ConversationItem conv)
    {
        ActiveConversation  = conv;
        conv.UnreadCount    = 0;
        SearchResults.Clear();
        SearchQuery = "";
        if (conv.IsGroup)
        {
            SendMode         = "All";
            RecipientUserId  = null;
        }
        else
        {
            SendMode         = "Direct";
            RecipientUserId  = conv.ContactName;
        }
        StateHasChanged();
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
                IsOnline        = OnlineUsers.Contains(name, StringComparer.OrdinalIgnoreCase),
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
        // Validate recipient still exists in Auth0 for direct messages
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
        var response = await _service!.CallApi(
            message!,
            Name,
            SendMode,
            IsDirectMode ? RecipientUserId : null
        );
        // Add sent message locally
        var sentMsg = new MessageRequest
        {
            message         = message,
            sendtime        = DateTime.Now.ToShortTimeString(),
            userid          = "You",
            recipientUserid = IsDirectMode ? RecipientUserId : null,
            sendmode        = SendMode
        };
        var key = ActiveConversation.ContactName;
        if (!ConversationMessages.ContainsKey(key))
            ConversationMessages[key] = new();
        ConversationMessages[key].Add(sentMsg);
        ActiveConversation.LastMessage     = message;
        ActiveConversation.LastMessageTime = sentMsg.sendtime;
        message = "";
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
                    var displayName = user.DisplayName;
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
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private void ListenUserListEvent()
    {
        signalRService.ListenUserListUpdated(onlineUserNames =>
        {
            OnlineUsers = onlineUserNames;
            foreach (var conv in Conversations.Where(c => !c.IsGroup))
            {
                conv.IsOnline = OnlineUsers.Contains(conv.ContactName, StringComparer.OrdinalIgnoreCase);
            }
            InvokeAsync(StateHasChanged);
        });
    }
}

