using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Request
{
    public class CreateGroupRequest
    {
        public string GroupName { get; set; }
        public string CreatedBy { get; set; } = "";
        public List<string> Members { get; set; } = new();
    }
}
