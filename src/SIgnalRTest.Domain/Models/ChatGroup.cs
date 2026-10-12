using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Models
{
    public  class ChatGroup
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string GroupName { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public List<string> Members { get; set; } = new();   // usernames
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
