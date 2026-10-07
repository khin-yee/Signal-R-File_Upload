using SIgnalRTest.Domain.Models;
using SIgnalRTest.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.IServices
{
    public interface ISignalRService
    {
        Task SendMessage(string groupId, string message, string userid, string sendMode, string? recipientUserId);
        ApiResponse MessageCreate(string groupId);
        Task<List<Auth0UserResponse>> GetAuth0Users(string? searchQuery = null);
        Task<Auth0UserResponse?> ValidateAuth0User(string usernameOrEmail);
        Task<List<ChatMessage>> GetMessages(string currentUserId,string sendMode,string? contactId = null,string groupId = "123",DateTime? after = null);
    }
}
