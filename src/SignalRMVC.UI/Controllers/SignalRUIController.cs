using Microsoft.AspNetCore.Mvc;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Request;

namespace SignalRMVC.UI.Controllers
{
    public class SignalRUIController : Controller
    {
        private readonly IApiCallService _apiService;

        private static MessageRequestDtos messages = new MessageRequestDtos();
        public SignalRUIController(IApiCallService apiService)
        {
            _apiService = apiService;
        }
        public IActionResult Index(MessageRequest request)
        {
            if (!string.IsNullOrEmpty(request.message) && request.userid != "UserMVC")
            {
                messages.Messages.Add(request);
            }
            return View(messages);
        }

        public  async Task<IActionResult> CallApi(string message)
        {            
            var MessageRequest = new MessageRequest
            {
                message =message,
                userid = "UserMVC",
                sendtime = DateTime.Now.ToShortTimeString()
            };       
            var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", MessageRequest);
            await _apiService.APICall(apiRequest);
            messages.Messages.Add(MessageRequest);
            return RedirectToAction("Index");
        }

    }
}
