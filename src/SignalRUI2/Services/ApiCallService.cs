using SIgnalRTest.Domain.Request;
using SIgnalRTest.Domain.Response;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace SignalRUI2.Services;
public class ApiCallService:IApiCallService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiCallService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiResponse> APICall(ApiRequest apiRequest)
    {
        var responseModel = new ApiResponse();
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(130);
            var request = PopulateHttpRequestMessage(apiRequest);
            if (!string.IsNullOrWhiteSpace(apiRequest.token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiRequest.token);
            }

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                using var contentStream =
                    await response.Content.ReadAsStreamAsync();

                StreamReader reader = new StreamReader(contentStream);
                responseModel = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse>(await reader.ReadToEndAsync())!;
            }
            else
            {
                responseModel.ErrorCode = "01";
                responseModel.ErrorMessage = response.StatusCode.ToString();
                responseModel.Detail = string.Empty;
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            responseModel.ErrorCode = "04";
            responseModel.ErrorMessage = ex.Message;
            responseModel.Detail = string.Empty;
        }
        catch (Exception ex)
        {
            responseModel.ErrorCode = "99";
            responseModel.ErrorMessage = ex.Message;
            responseModel.Detail = string.Empty;
        }
        return responseModel;
    }

    private HttpRequestMessage PopulateHttpRequestMessage(ApiRequest apiRequest)
    {
        bool isFormContent = (apiRequest.requestBody?.GetType()== typeof(MultipartFormDataContent));
        var baseUrl = $"{_configuration["ApiURl:baseurl"]}{apiRequest.url}";
        var httpRequestMsg = new HttpRequestMessage(apiRequest.method, baseUrl)
        {
            Content = (isFormContent) ?
           (apiRequest.requestBody as MultipartFormDataContent) : new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(apiRequest.requestBody), Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        httpRequestMsg.Headers.UserAgent.ParseAdd("HttpRequestsSample");
        httpRequestMsg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(isFormContent ? "*/*" : "application/json"));
        return httpRequestMsg;

    }
}

