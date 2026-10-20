using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Request
{
    public class UpdateGroupRequest
    {
        public string GroupId { get; set; } = "";
        public string RequestedBy { get; set; } = "";  // must match CreatedBy to be allowed
        public string? NewName { get; set; }            // null = no name change
        public List<string> MembersToAdd { get; set; } = new();
    }
}
