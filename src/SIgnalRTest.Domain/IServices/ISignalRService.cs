using SIgnalRTest.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.IServices
{
    public interface ISignalRService
    {
        Task SendMessage(string groupId);
        Task<ApiResponse> MessageCreate(string groupId);
    }
}
