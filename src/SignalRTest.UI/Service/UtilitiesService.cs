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

        public async Task<ApiResponse> CallApi(string message,string username = "User2",string sendMode = "All", string? recipientUserId = null,string? groupId = null)          // ✅ NEW param
        {
            var MessageRequest = new MessageRequest
            {
                message         = message,
                userid          = username,
                sendtime        = DateTime.Now.ToShortTimeString(),
                sendmode        = sendMode,
                recipientUserid = recipientUserId,
                groupId         = groupId    // ✅ NEW
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

        public async Task<ApiResponse> GetMessages(string currentUserId, string sendMode = "All",string? contactId = null, string groupId = "123",DateTime? after = null)
        {
            string url;

            if (sendMode == "All" || sendMode == "Group")
                url = $"/GetMessages?currentUserId={Uri.EscapeDataString(currentUserId)}" +
                      $"&sendMode={sendMode}&groupId={Uri.EscapeDataString(groupId)}";
            else
                url = $"/GetMessages?currentUserId={Uri.EscapeDataString(currentUserId)}" +
                      $"&sendMode=Direct&contactId={Uri.EscapeDataString(contactId ?? "")}";

            if (after.HasValue)
                url += $"&after={Uri.EscapeDataString(after.Value.ToString("o"))}";

            return await _apiCallService.APICall(new ApiRequest(HttpMethod.Get, url, null));
        }

        public async Task<ApiResponse> CreateGroup(string groupName, string createdBy, List<string> members)
        {
            var body = new { GroupName = groupName, CreatedBy = createdBy, Members = members };
            return await _apiCallService.APICall(
                new ApiRequest(HttpMethod.Post, "/CreateGroup", body));
        }
        public async Task<ApiResponse> GetGroups(string username)
        {
            var url = $"/GetGroups?username={Uri.EscapeDataString(username)}";
            return await _apiCallService.APICall(new ApiRequest(HttpMethod.Get, url, null));
        }
    }
}
