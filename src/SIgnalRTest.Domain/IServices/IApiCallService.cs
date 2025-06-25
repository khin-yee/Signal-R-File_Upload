using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.IServices
{
    public interface IApiCallService
    {
        Task<ApiResponse> APICall(ApiRequest apiRequest);

    }
}
