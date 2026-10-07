using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SignalRTest.Service.SignalRClient;
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

    [HttpGet("/GetUsers")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null)
    {
        var users = await _service.GetAuth0Users(search);
        // Mark each user as online if their DisplayName is in the SignalR registry
        var onlineUsers = SignalRHub.GetOnlineUsers()
            .Select(u => u.ToLower())
            .ToHashSet();
        foreach (var user in users)
        {
            user.IsOnline = onlineUsers.Contains(user.DisplayName.ToLower())
                         || onlineUsers.Contains(user.Email.ToLower());
        }
        return Ok(users);
    }

    [HttpGet("/ValidateUser")]
    public async Task<IActionResult> ValidateUser([FromQuery] string usernameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return BadRequest(new ApiResponse { ErrorCode = "02", ErrorMessage = "usernameOrEmail is required" });
        var user = await _service.ValidateAuth0User(usernameOrEmail);
        if (user == null)
            return NotFound(new ApiResponse { ErrorCode = "03", ErrorMessage = $"User '{usernameOrEmail}' not found in Auth0" });
        return Ok(user);
    }

    [HttpGet("/GetMessages")]
    public async Task<IActionResult> GetMessages(
    [FromQuery] string currentUserId,
    [FromQuery] string sendMode = "All",
    [FromQuery] string? contactId = null,
    [FromQuery] string groupId = "123",
    [FromQuery] string? after = null)   // ISO 8601 UTC string
    {
        // Parse the "after" timestamp for incremental sync
        DateTime? afterDate = null;
        if (!string.IsNullOrEmpty(after) &&
            DateTime.TryParse(after, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            afterDate = parsed;
        }

        var messages = await _service.GetMessages(
            currentUserId, sendMode, contactId, groupId, afterDate);

        return Ok(messages);
    }
}

