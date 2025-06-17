using Microsoft.AspNetCore.Components;
using MudBlazor;
using SignalRTest.UI.Service;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using System.Text.RegularExpressions;

namespace SignalRTest.UI.Components.Pages;
public partial class SignalR : ComponentBase
{
    public string? currentmessage { get; set; }
    public string? author { get; set; } = "other";
    public string? message { get; set; }
    public MessageRequest? messageRequest { get; set; } = new MessageRequest();
    public List<MessageRequest> messages { get; set; } = new List<MessageRequest>();
    [Inject]
    public UtilitiesService? _service { get; set; }
    [Inject]
    public required SignalRService signalRService { get; set; }
    protected override async Task OnInitializedAsync()
    {
        await signalRService.StartAsync();
        await signalRService.JoinGroupAsync("123");
        ListenSignalREvent();
        await base.OnInitializedAsync();
    }
    private void ListenSignalREvent()
    {
        signalRService.ReceiveTwoMessageAsync<string, string>("ReceiveMessage", (message, userid) =>
        {
            var messageRequest = new MessageRequest
            {
                message = message
            };
            if (userid == "User1")
            {
                userid = "You";
            }
            messageRequest!.userid = userid;
            messages.Add(messageRequest);
            Console.WriteLine($"Received message from {userid}: {message}");
            InvokeAsync(StateHasChanged);
        });
    }
    public async Task<ApiResponse> CallApi()
    {
        currentmessage =  "Calling SignalR.....";
        author = "You";
        var response = await _service!.CallApi(message!);
        await signalRService.JoinGroupAsync(response.Detail!);
        return response;
    }
}

