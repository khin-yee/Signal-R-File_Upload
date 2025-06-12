using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignalRTest.Service.SignalRClient;
public class SignalRHub : Hub
{
    private readonly IHubContext<SignalRHub> _singnalRhub;

    public SignalRHub (IHubContext<SignalRHub> singnalRhub)
    {
        _singnalRhub = singnalRhub;
    } 
    public async Task SendSignalR(string groupId, string method,string message,string userid)
    {
        await _singnalRhub.Clients.Group(groupId).SendAsync(method, message,userid);
    }

    public async Task SendAll(string method, string message)
    {
        await _singnalRhub.Clients.All.SendAsync(method, message);
    }
    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }
}
