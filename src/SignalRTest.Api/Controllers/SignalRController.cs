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
        var groupId = request!.groupId ?? "123";
        _service.SendMessage(groupId, request.message!, request.userid,
                             request.sendmode, request.recipientUserid);
        return Ok(new ApiResponse { Detail = groupId });
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

    [HttpPost("/CreateGroup")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupName))
            return BadRequest(new ApiResponse
            { ErrorCode = "02", ErrorMessage = "Group name is required" });
        if (!request.Members.Any())
            return BadRequest(new ApiResponse
            { ErrorCode = "02", ErrorMessage = "At least one member is required" });
        var group = await _service.CreateGroup(
            request.GroupName, request.CreatedBy, request.Members);
        return Ok(group);
    }

    [HttpGet("/GetGroups")]
    public async Task<IActionResult> GetGroups([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new ApiResponse
            { ErrorCode = "02", ErrorMessage = "username is required" });
        var groups = await _service.GetGroups(username);
        return Ok(groups);
    }

    [HttpPut("/UpdateGroup")]
    public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupRequest request)
    {
        if (string.IsNullOrEmpty(request.GroupId))
            return BadRequest(new ApiResponse { ErrorCode= "02",ErrorMessage = "ID required!"});
        var result = await _service.UpdateGroup(request.GroupId, request.RequestedBy, request.NewName, request.MembersToAdd);
        if (result ==null)
            return StatusCode(403, new ApiResponse { ErrorCode = "03", ErrorMessage = "only group admin can update" });
        return Ok(result);
    }

    // ✅ NEW — admin removes a member
    [HttpPost("/RemoveGroupMember")]
    public async Task<IActionResult> RemoveGroupMember([FromBody] RemoveMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupId) ||
            string.IsNullOrWhiteSpace(request.MemberToRemove))
            return BadRequest(new ApiResponse
                { ErrorCode = "02", ErrorMessage = "GroupId and MemberToRemove are required" });

        var result = await _service.RemoveMember(
            request.GroupId, request.RequestedBy, request.MemberToRemove);

        if (result == null)
            return StatusCode(403, new ApiResponse
                { ErrorCode = "03", ErrorMessage = "Only the group admin can remove members" });

        return Ok(result);
    }

    [HttpPost("/LeaveGroup")]
    public async Task<IActionResult> LeaveGroup([FromBody] LeaveGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupId) ||
            string.IsNullOrWhiteSpace(request.MemberName))
            return BadRequest(new ApiResponse
            { ErrorCode = "02", ErrorMessage = "GroupId and MemberName are required" });

        await _service.LeaveGroup(request.GroupId, request.MemberName);
        return Ok();
    }
}

