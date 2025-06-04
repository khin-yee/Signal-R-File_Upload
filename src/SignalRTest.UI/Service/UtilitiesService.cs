using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;

namespace SignalRTest.UI.Service
{
    public class UtilitiesService
    {
        private readonly IApiCallService _apiCallService;

        public UtilitiesService (IApiCallService apiCallService)
        {
            _apiCallService = apiCallService;
        }
        public async Task<ApiResponse> CallApi()
        {
            var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", "", "");
            return await _apiCallService.APICall(apiRequest);
        }
    }
}
