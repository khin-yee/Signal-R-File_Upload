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

    public readonly SignalRHub _signalR;

    public SignalRService (SignalRHub signalR)
    {
        _signalR = signalR;
    } 
    public async Task<ApiResponse> MessageCreate(string groupId)
    {
        await _signalR.SendSignalR(groupId, "First");
        var response = new ApiResponse();
        return response;
    }
    public async Task SendMessage(string groupId)
    {
        await _signalR.SendSignalR(groupId, "Hello");
        await Task.Delay(2000);
        await _signalR.SendSignalR(groupId, "Second");
        await Task.Delay(2000);
        await _signalR.SendSignalR(groupId, "Third");
        await Task.Delay(3000);
    }

    
}

