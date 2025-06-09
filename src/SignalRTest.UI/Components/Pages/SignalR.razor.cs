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

    [Inject]
    public UtilitiesService? _service { get; set; }

    [Inject]
    public required SignalRService signalRService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await signalRService.StartAsync();
        await base.OnInitializedAsync();
    }

    private void ListenSignalREvent()
    {
        signalRService.ReceiveMessageAsync<string>("ReceiveMessage", message =>
        {
            currentmessage = message;
            Console.WriteLine($"Received message: {currentmessage}");
            InvokeAsync(StateHasChanged);
        });
    }
    public async Task<ApiResponse> CallApi()
    {
        var response = await _service!.CallApi();

        await signalRService.JoinGroupAsync(response.Detail!);
        ListenSignalREvent();
        return response;
    }
}

