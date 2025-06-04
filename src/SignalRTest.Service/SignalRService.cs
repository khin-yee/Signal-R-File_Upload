using Microsoft.AspNetCore.SignalR;
using SignalRTest.Service.SignalRClient;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignalRTest.Service;
public class SignalRService:ISignalRService
{
    private readonly IHubContext<SignalRHub> _singnalRhub;

    public SignalRService (IHubContext<SignalRHub> signalRHub)
    {
        _singnalRhub = signalRHub;
    } 
    public async Task<ApiResponse> MessageCreate(string groupId)
    {
        await SendSignalR(groupId, "First");
        var response = new ApiResponse();
        return response;
    }
    public async Task SendMessage(string groupId)
    {
        await SendSignalR(groupId, "Hello");
        await Task.Delay(2000);
        await SendSignalR(groupId, "Second");
        await Task.Delay(3000);
        await SendSignalR(groupId, "Third");
        await Task.Delay(3000);
    }

    public async Task SendSignalR (string groupId,string message)
    {
        await _singnalRhub.Clients.Group(groupId).SendAsync("ReceiveMessage", message);
    }
}

