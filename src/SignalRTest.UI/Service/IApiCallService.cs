using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;

namespace SignalRTest.UI.Service
{
    public interface IApiCallService
    {
        Task<ApiResponse> APICall(ApiRequest apiRequest);
    }
}
