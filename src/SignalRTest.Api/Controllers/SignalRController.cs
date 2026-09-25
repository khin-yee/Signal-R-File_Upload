using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using System.Security.Cryptography.X509Certificates;

namespace SignalRTest.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public class SignalRController : ControllerBase
{
    private readonly ISignalRService _service;
    private readonly IBackgroundJobClient _backgroundJob;


    public SignalRController(ISignalRService service, IBackgroundJobClient backgroundJob)
    {
        _service = service;
        _backgroundJob = backgroundJob;
    }

    [HttpPost("/TestSignalR")]
    public IActionResult GetStatus([FromBody]MessageRequest? request)
    {
        var groupId = Guid.NewGuid().ToString();
        //_backgroundJob.Enqueue<ISignalRService>(s =>
        //     s.SendMessage(
        //         groupId,
        //         request!.message!,
        //         request.userid,
        //         request.sendmode,         
        //         request.recipientUserid
        //     ));
        _service.SendMessage(groupId,request!.message!,request.userid,request.sendmode,request.recipientUserid);
        var apiresponse = new ApiResponse() { Detail = groupId };
        return Ok(apiresponse);
    }
}

