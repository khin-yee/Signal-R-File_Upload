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
    public ApiResponse MessageCreate(string groupId)
    {
        var response = new ApiResponse();
        return response;
    }
    public async Task SendMessage(string groupId,string message,string userid)
    {
        await _signalR.SendAll("GroupId", groupId);
        //await _signalR.SendAll("ReceiveMessage", message);
        await _signalR.SendSignalR("123", "ReceiveMessage", message, userid);
        //await Task.Delay(2000);
        //await _signalR.SendSignalR(groupId, "ReceiveMessage", message);
        //await Task.Delay(2000);
        //await _signalR.SendSignalR(groupId, "ReceiveMessage", message);
        //await Task.Delay(3000);
    }

    
}

