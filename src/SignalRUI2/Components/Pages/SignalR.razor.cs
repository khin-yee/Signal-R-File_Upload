using Microsoft.AspNetCore.Components;
using MudBlazor;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using SignalRUI2.Services;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SignalRUI2.Components.Pages;

public partial class SignalR : ComponentBase
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string Name { get; set; }
    public string? currentmessage { get; set; }

    public string? author { get; set; } = "others";
    public string? groupId { get; set; }
    public string? message { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;
    public List<MessageRequest> messages { get; set; } = new List<MessageRequest>();

    public string SendMode { get; set; } = "All";
    public string? RecipientUserId { get; set; }
    public bool IsDirectMode => SendMode == "Direct";
    private const string MyUsername = "User2";


    [Inject]
    public UtilitiesService? _service { get; set; }

    [Inject]
    public required SignalRService signalRService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await signalRService.StartAsync();
        if (string.IsNullOrEmpty(groupId))
        {
            await signalRService.JoinGroupAsync("123");
        }
        await signalRService.RegisterUserAsync(MyUsername);
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
                sendtime = sendtime,
            };
            if (userid != "User2")
            {
                Snackbar.Add($"You got new message from "+ userid, Severity.Success);

                messageRequest!.userid = userid;

                messages.Add(messageRequest);
                Console.WriteLine($"Received message from {userid}: {message}");
            }
                InvokeAsync(StateHasChanged);
            
        });
    }

    public async Task<ApiResponse> CallApi()
    {
        currentmessage = "Calling SignalR.....";
        author = "You";
        var messageRequest = new MessageRequest
        {
            message = message,
            sendtime = DateTime.Now.ToShortTimeString(),
            userid = "You",
            sendmode = SendMode,
            recipientUserid = IsDirectMode ? RecipientUserId : null
        };
        messages.Add(messageRequest);
        var response = await _service!.CallApi(
            message,
            MyUsername,
            SendMode,
            IsDirectMode ? RecipientUserId : null
        );
        StateHasChanged();
        message = "";
        return response;
    }
}

