using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Response
{
    public  class ApiResponse
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string? Detail { get; set; }

    }
}
