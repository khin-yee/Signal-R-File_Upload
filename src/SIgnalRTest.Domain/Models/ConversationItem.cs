using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Models
{

    public class ConversationItem
    {
        public string ContactName { get; set; } = "";      // username or "Group"
        public string ContactEmail { get; set; } = "";     // from Auth0
        public bool IsGroup { get; set; } = false;         // true = Group Chat
        public bool IsOnline { get; set; } = false;        // from SignalR registry
        public string LastMessage { get; set; } = "";      // preview text
        public string LastMessageTime { get; set; } = "";
        public int UnreadCount { get; set; } = 0;          // badge number
        public string AvatarColor { get; set; } = "#ede9fe"; // bg colour
        public string AvatarTextColor { get; set; } = "#4f46e5";
        public DateTime? LastSeen { get; set; } = null;
    }
}
