using System.Net.Http.Headers;
using System.Text.Json;
using ProjectService.DTOs;

namespace ProjectService.Services;

public interface ITaskServiceClient
{
    Task<List<FMNTaskDto>> GetTasksByProjectIdAsync(int projectId, string authToken);
    Task<List<FMNTaskDto>> GetTasksByUserAsync(string authToken);
}

public class TaskServiceClient : ITaskServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TaskServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TaskServiceClient(HttpClient httpClient, ILogger<TaskServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<FMNTaskDto>> GetTasksByProjectIdAsync(int projectId, string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/FMNTasks/byproject/{projectId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get tasks for project {ProjectId}: {StatusCode}", projectId, response.StatusCode);
                return new List<FMNTaskDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<FMNTaskDto>>(content, _jsonOptions) ?? new List<FMNTaskDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tasks for project {ProjectId}", projectId);
            return new List<FMNTaskDto>();
        }
    }

    public async Task<List<FMNTaskDto>> GetTasksByUserAsync(string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/FMNTasks");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get tasks: {StatusCode}", response.StatusCode);
                return new List<FMNTaskDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<FMNTaskDto>>(content, _jsonOptions) ?? new List<FMNTaskDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tasks");
            return new List<FMNTaskDto>();
        }
    }
}
