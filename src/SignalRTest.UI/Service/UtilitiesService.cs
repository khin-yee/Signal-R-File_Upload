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
        public async Task<ApiResponse> CallApi(string message)
        {
            var MessageRequest = new MessageRequest
            {
                message =message,
                userid = "User1",
            };
            var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", MessageRequest);
            return await _apiCallService.APICall(apiRequest);
        }
    }
}
