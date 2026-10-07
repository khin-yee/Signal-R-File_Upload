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
        public async Task<ApiResponse> CallApi(string message,string username)
        {
            var MessageRequest = new MessageRequest
            {
                message =message,
                userid = username,
            };
            var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", MessageRequest);
            return await _apiCallService.APICall(apiRequest);
        }

        public async Task<ApiResponse> CallApi(string message, string username = "User2", string sendMode = "All", string? recipientUserId = null)
        {
            var MessageRequest = new MessageRequest
            {
                message = message,
                userid = username,                    
                sendtime = DateTime.Now.ToShortTimeString(),
                sendmode = sendMode,
                recipientUserid = recipientUserId    
            };
            var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", MessageRequest);
            return await _apiCallService.APICall(apiRequest);
        }

        public async Task<ApiResponse> GetUsers(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search)
                ? "/GetUsers"
                : $"/GetUsers?search={Uri.EscapeDataString(search)}";
            var apiRequest = new ApiRequest(HttpMethod.Get, url, null);
            return await _apiCallService.APICall(apiRequest);
        }

        public async Task<ApiResponse> ValidateUser(string usernameOrEmail)
        {
            var url = $"/ValidateUser?usernameOrEmail={Uri.EscapeDataString(usernameOrEmail)}";
            var apiRequest = new ApiRequest(HttpMethod.Get, url, null);
            return await _apiCallService.APICall(apiRequest);
        }

        public async Task<ApiResponse> GetMessages(string currentUserId,string sendMode = "All",string? contactId = null,string groupId = "123",DateTime? after = null)
        {
            var url = sendMode == "All"
                ? $"/GetMessages?currentUserId={Uri.EscapeDataString(currentUserId)}" +
                  $"&sendMode=All&groupId={groupId}"
                : $"/GetMessages?currentUserId={Uri.EscapeDataString(currentUserId)}" +
                  $"&sendMode=Direct&contactId={Uri.EscapeDataString(contactId ?? "")}";
            // Append after= for incremental sync
            if (after.HasValue)
                url += $"&after={Uri.EscapeDataString(after.Value.ToString("o"))}";
            var apiRequest = new ApiRequest(HttpMethod.Get, url, null);
            return await _apiCallService.APICall(apiRequest);
        }
    }
}
