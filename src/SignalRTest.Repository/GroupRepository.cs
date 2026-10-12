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
    }
}
