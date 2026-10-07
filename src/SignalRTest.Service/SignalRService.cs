using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalRTest.Service.SignalRClient;
using SIgnalRTest.Domain.IServices;
using SIgnalRTest.Domain.Models;
using SIgnalRTest.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignalRTest.Service;
public class SignalRService:ISignalRService
{
    private readonly IConfiguration _configuration;

    public readonly SignalRHub _signalR;

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IMessageRepository _messageRepository;
    public SignalRService (SignalRHub signalR,IConfiguration configuration, IHttpClientFactory httpClientFactory, IMessageRepository messageRepo)
    {
        _signalR = signalR;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _messageRepository = messageRepo;
    }
    public ApiResponse MessageCreate(string groupId)
    {
        return new ApiResponse();
    }
    public async Task SendMessage(string groupId, string message, string userid, string sendMode, string? recipientUserId)
    {
        await _messageRepository.SaveAsync(new ChatMessage
        {
            SenderId      = userid,
            RecipientId   = sendMode == "Direct" ? recipientUserId : null,
            SendMode      = sendMode,
            GroupId       = groupId,
            Message       = message,
            SentAt        = DateTime.UtcNow,
            SentAtDisplay = DateTime.Now.ToShortTimeString()
        });

        if (sendMode == "Direct" && !string.IsNullOrEmpty(recipientUserId))
        {
            await _signalR.SendToUser(recipientUserId, "ReceiveMessage", message, userid);
        }
        else
        {
            await _signalR.SendSignalR("123", "ReceiveMessage", message, userid);
        }
    }

    private async Task<string> GetManagementApiToken()
    {
        var domain = _configuration["Auth0:Domain"];
        var clientId = _configuration["Auth0:ClientId"];
        var secret = _configuration["Auth0:ClientSecret"];
        var audience = _configuration["Auth0:Audience"];
        var client = _httpClientFactory.CreateClient();
        var payload = new
        {
            grant_type = "client_credentials",
            client_id = clientId,
            client_secret = secret,
            audience = audience
        };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://{domain}/oauth/token", content);
        var json = await response.Content.ReadAsStringAsync();
        var token = JObject.Parse(json)["access_token"]?.ToString();
        return token ?? throw new Exception("Failed to obtain Auth0 management token.");
    }
    public async Task<List<Auth0UserResponse>> GetAuth0Users(string? searchQuery = null)
    {
        var domain = _configuration["Auth0:Domain"];
        var token = await GetManagementApiToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        string url;
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var encoded = Uri.EscapeDataString(searchQuery);
            url = $"https://{domain}/api/v2/users?q=email:*{encoded}*+OR+name:*{encoded}*&search_engine=v3&per_page=50";
        }
        else
        {
            url = $"https://{domain}/api/v2/users?per_page=50&include_totals=false";
        }
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<Auth0UserResponse>>(json)
               ?? new List<Auth0UserResponse>();
    }
    public async Task<Auth0UserResponse?> ValidateAuth0User(string usernameOrEmail)
    {
        var users = await GetAuth0Users(usernameOrEmail);
        return users?.FirstOrDefault();
    }
    public async Task<List<ChatMessage>> GetMessages(string currentUserId, string sendMode,string? contactId = null, string groupId = "123",DateTime? after = null)
    {
        if (sendMode == "All")
            return await _messageRepository.GetGroupMessagesAsync(groupId, after);

        if (!string.IsNullOrEmpty(contactId))
            return await _messageRepository.GetDirectMessagesAsync(currentUserId, contactId, after);

        return new List<ChatMessage>();
    }
}

