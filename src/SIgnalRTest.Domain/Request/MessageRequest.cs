using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Request
{
    public  class MessageRequest
    {
        public string? message { get; set; }

        public string userid { get; set; }

        public string? sendtime { get; set; }
    }
}
