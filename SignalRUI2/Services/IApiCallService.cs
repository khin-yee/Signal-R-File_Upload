using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;

namespace SignalRUI2.Services
{
    public interface IApiCallService
    {
       Task<ApiResponse> APICall(ApiRequest apiRequest);
    }
}
