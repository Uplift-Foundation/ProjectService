using System.Net.Http.Headers;
using System.Text.Json;
using ProjectService.DTOs;

namespace ProjectService.Services;

public interface IHabitServiceClient
{
    Task<List<HabitDto>> GetHabitsByProjectIdAsync(int projectId, string authToken);
    Task<List<HabitDto>> GetHabitsByUserAsync(string authToken);
}

public class HabitServiceClient : IHabitServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HabitServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HabitServiceClient(HttpClient httpClient, ILogger<HabitServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<HabitDto>> GetHabitsByProjectIdAsync(int projectId, string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/Habits/byproject/{projectId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get habits for project {ProjectId}: {StatusCode}", projectId, response.StatusCode);
                return new List<HabitDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HabitDto>>(content, _jsonOptions) ?? new List<HabitDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching habits for project {ProjectId}", projectId);
            return new List<HabitDto>();
        }
    }

    public async Task<List<HabitDto>> GetHabitsByUserAsync(string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/Habits");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get habits: {StatusCode}", response.StatusCode);
                return new List<HabitDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HabitDto>>(content, _jsonOptions) ?? new List<HabitDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching habits");
            return new List<HabitDto>();
        }
    }
}
