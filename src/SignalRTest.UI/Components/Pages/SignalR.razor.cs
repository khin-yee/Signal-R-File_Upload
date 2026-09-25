using Microsoft.AspNetCore.Components;
using MudBlazor;
using SignalRTest.UI.Service;
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
        ListenSignalREvent();
        await base.OnInitializedAsync();
    }
    private void ListenSignalREvent()
    {
        signalRService.ReceiveTwoMessageAsync<string, string,string>("ReceiveMessage", (message, userid,sendtime) =>
        {
            var messageRequest = new MessageRequest
            {
                message = message,
                sendtime = sendtime
            };
            if (userid != Name)
            {
                Snackbar.Add($"You got new message from "+ userid, Severity.Success);
                messageRequest!.userid = userid;
                messages.Add(messageRequest);
            }
            InvokeAsync(StateHasChanged);
        });
    }
    public async Task<ApiResponse> CallApi()
    {
        currentmessage = "Calling SignalR.....";
        author = "You";
        var response = await _service!.CallApi(
            message!,
            Name,
            SendMode,                              
            IsDirectMode ? RecipientUserId : null  
        );
        var messageRequest = new MessageRequest
        {
            message = message,
            sendtime = DateTime.Now.ToShortTimeString(),
            userid = "You",
            recipientUserid = IsDirectMode ? RecipientUserId : null,
            sendmode = SendMode
        };
        messages.Add(messageRequest);
        message = "";
        return response;
    }
}

