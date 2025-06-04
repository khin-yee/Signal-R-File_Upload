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
    public async Task SendAllMessage(string message, object request)
    {
        await Clients.All.SendAsync(message, request);
    }

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }
}
