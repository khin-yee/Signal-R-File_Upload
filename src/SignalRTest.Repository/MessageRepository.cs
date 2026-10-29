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
    public  class MessageRepository:IMessageRepository
    {
        private readonly IMongoCollection<ChatMessage> _collection;
        public MessageRepository(IMongoClient mongoClient, IConfiguration configuration)
        {
            var dbName = configuration["DatabaseOptions:DatabaseName"] ?? "TicketInventorySystem";
            var db = mongoClient.GetDatabase(dbName);
            _collection = db.GetCollection<ChatMessage>("ChatMessages");
            var indexKey = Builders<ChatMessage>.IndexKeys.Ascending(m => m.SentAt);
            var indexOpts = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90) };
            _collection.Indexes.CreateOne(new CreateIndexModel<ChatMessage>(indexKey, indexOpts));
        }
        public async Task SaveAsync(ChatMessage message)
        {
            await _collection.InsertOneAsync(message);
        }
 
        public async Task<List<ChatMessage>> GetGroupMessagesAsync(string groupId, DateTime? after = null, int limit = 50)
        {
            // Filter by GroupId — unique per conversation (either "123" or MongoDB ObjectId)
            var filter = Builders<ChatMessage>.Filter.Eq(m => m.GroupId, groupId);
            if (after.HasValue)
                filter = Builders<ChatMessage>.Filter.And(filter,Builders<ChatMessage>.Filter.Gt(m => m.SentAt, after.Value));

            //var test = new AggregateBucketAutoResult<>
            return await _collection.Find(filter).SortBy(m => m.SentAt).Limit(limit).ToListAsync();
        }
        public async Task<List<ChatMessage>> GetDirectMessagesAsync(string userId, string contactId, DateTime? after = null, int limit = 50)
        {
            var directionFilter = Builders<ChatMessage>.Filter.Or(Builders<ChatMessage>.Filter.And(
                    Builders<ChatMessage>.Filter.Eq(m => m.SenderId, userId),
                    Builders<ChatMessage>.Filter.Eq(m => m.RecipientId, contactId)
                ),
                Builders<ChatMessage>.Filter.And(Builders<ChatMessage>.Filter.Eq(m => m.SenderId, contactId),
                    Builders<ChatMessage>.Filter.Eq(m => m.RecipientId, userId)
                )
            );
            var filter = directionFilter;
            if (after.HasValue)
            {
                filter = Builders<ChatMessage>.Filter.And(
                    directionFilter,
                    Builders<ChatMessage>.Filter.Gt(m => m.SentAt, after.Value)
                );
            }
           
                return await _collection.Find(filter).SortBy(m => m.SentAt).Limit(limit).ToListAsync();
        }
    }
}
