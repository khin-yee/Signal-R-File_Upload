using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Response
{
    public class Auth0UserResponse
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; } = "";
        [JsonProperty("username")]
        public string? Username { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; } = "";
        [JsonProperty("name")]
        public string? Name { get; set; }
        // Not from Auth0 — set by our server based on SignalR registry
        public bool IsOnline { get; set; } = false;
        // Helper — returns Username if set, otherwise the part before @ in Email
        public string DisplayName => !string.IsNullOrEmpty(Username)
            ? Username
            : Email.Split('@')[0];
    }
}
