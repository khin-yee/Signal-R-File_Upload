using SIgnalRTest.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.IServices
{
    public interface IGroupRepository
    {
        Task<ChatGroup> CreateAsync(ChatGroup group);
        Task<List<ChatGroup>> GetByMemberAsync(string username);
        Task<ChatGroup?> GetByIdAsync(string groupId);
        Task UpdateNameAsync(string groupId, string newName);
        Task<ChatGroup?> AddMembersAsync(string groupId, List<string> newMembers);

        // ✅ NEW — pull one member from the Members array
        Task<ChatGroup?> RemoveMemberAsync(string groupId, string memberToRemove);
        Task<ChatGroup?> LeaveGroupAsync(string groupId, string memberName);
        Task TransferAdminAsync(string groupId, string newAdminName);
        Task DeleteAsync(string groupId);
    }
}
