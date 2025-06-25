using Microsoft.AspNetCore.Mvc;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Request;

namespace SignalRMVC.UI.Controllers
{
    public class SignalRUIController : Controller
    {
        private readonly IApiCallService _apiService;
        public SignalRUIController(IApiCallService apiService)
        {
            _apiService = apiService;
        }
        public IActionResult Index(MessageRequest request)
        {
            return View();
        }

        public  async Task<IActionResult> CallApi(string message)
        {
            var MessageRequest = new MessageRequest
            {
                message =message,
                userid = "UserMVC",
            };
            var apiRequest = new ApiRequest(HttpMethod.Post, "/TestSignalR", MessageRequest);
             await _apiService.APICall(apiRequest);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveMessage([FromBody] MessageRequest request)
        {
            MessageRequestDtos messages = new MessageRequestDtos();
            messages.Messages.Add(request);
            return RedirectToAction("Index");
        }


    }
}
