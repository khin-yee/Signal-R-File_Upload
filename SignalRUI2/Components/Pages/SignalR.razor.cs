using Microsoft.AspNetCore.Components;
using SIgnalRTest.Domain.Response;
using SignalRUI2.Services;

namespace SignalRUI2.Components.Pages;

public partial class SignalR : ComponentBase
{
    public string? currentmessage { get; set; }
    public string? groupId { get; set; }
    public string?message { get; set; }
     

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
        ListenSignalREvent();
        await base.OnInitializedAsync();
    }

    private void ListenSignalREvent()
    {
        signalRService.ReceiveMessageAsync<string>("GroupId", groupid =>
        {
            groupId = groupid;
            InvokeAsync(StateHasChanged);

        });
        signalRService.ReceiveMessageAsync<string>("ReceiveMessage", message =>
        {
            currentmessage = message;
            Console.WriteLine($"Received message: {currentmessage}");
            InvokeAsync(StateHasChanged);
        });
    }
    public async Task<ApiResponse> CallApi()
    {
        currentmessage =  "Calling SignalR.....";
        var response = await _service!.CallApi(message);

        await signalRService.JoinGroupAsync(response.Detail!);
        ListenSignalREvent();
        return response;
    }
}

