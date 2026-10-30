using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRTest.Repository
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IMongoCollection<ChatGroup> _collection;
        public GroupRepository(IMongoClient mongoClient,IConfiguration configuration)
        {
            var dbName = configuration["DatabaseOptions:DatabaseName"] ?? "TicketInventorySystem";
            _collection = mongoClient
                .GetDatabase(dbName)
                .GetCollection<ChatGroup>("ChatGroups");
        }
        public async Task<ChatGroup> CreateAsync(ChatGroup group)
        {
            await _collection.InsertOneAsync(group);
            return group;  
        }
       
        public async Task<ChatGroup?> GetByIdAsync(string groupId)
        {
            return await _collection.Find(g => g.Id == groupId).FirstOrDefaultAsync();
        }

        public async Task<List<ChatGroup>> GetByMemberAsync(string username)
        {
            return await _collection.Find(g => g.Members.Contains(username))
           .SortByDescending(g => g.CreatedAt)
           .ToListAsync();
        }
        public async Task UpdateNameAsync(string groupId,string newName)
        {
            await _collection.UpdateOneAsync(g => g.Id == groupId,Builders<ChatGroup>.Update.Set(g=>g.GroupName,newName));
        }

        public async Task<ChatGroup?> AddMembersAsync(string groupId,List<string> newMembers)
        {
            var update = Builders<ChatGroup>.Update.AddToSetEach(g => g.Members, newMembers);
            await _collection.UpdateOneAsync(g => g.Id ==groupId, update);
            return await GetByIdAsync(groupId);
        }

        public async Task<ChatGroup?> RemoveMemberAsync(string groupId, string memberToRemove)
        {
            var update = Builders<ChatGroup>.Update.Pull(g => g.Members, memberToRemove);
            await _collection.UpdateOneAsync(g => g.Id == groupId, update);
            return await GetByIdAsync(groupId);
        }

        public async Task<ChatGroup?> LeaveGroupAsync(string groupId, string memberName)
        {
            var update = Builders<ChatGroup>.Update.Pull(g => g.Members, memberName);
            await _collection.UpdateOneAsync(g => g.Id == groupId, update);
            return await GetByIdAsync(groupId);
        }

        public async Task TransferAdminAsync(string groupId, string newAdminName)
        {
            var update = Builders<ChatGroup>.Update.Set(g => g.CreatedBy, newAdminName);
            await _collection.UpdateOneAsync(g => g.Id == groupId, update);
        }

        public async Task DeleteAsync(string groupId)
        {
            await _collection.DeleteOneAsync(g => g.Id == groupId);
        }
    }
}
