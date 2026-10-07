using SIgnalRTest.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.IServices
{
    public interface IMessageRepository
    {
        Task SaveAsync(ChatMessage message);
        Task<List<ChatMessage>> GetGroupMessagesAsync(
            string groupId,
            DateTime? after = null,
            int limit = 50);
        Task<List<ChatMessage>> GetDirectMessagesAsync(
            string userId,
            string contactId,
            DateTime? after = null,
            int limit = 50);
    }
}
