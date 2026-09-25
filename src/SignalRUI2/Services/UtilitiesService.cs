using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;

namespace SignalRUI2.Services;
public class UtilitiesService
{
    private readonly IApiCallService _apiCallService;

    public UtilitiesService(IApiCallService apiCallService)
    {
        _apiCallService = apiCallService;
    }
    public async Task<ApiResponse> CallApi(string message, string username, string sendMode = "All", string? recipientUserId = null)
    {
        var MessageRequest = new MessageRequest
        {
            message = message,
            userid = username,
            sendmode = sendMode,
            recipientUserid = recipientUserId    
        };
        var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", MessageRequest);
        return await _apiCallService.APICall(apiRequest);
    }
}

