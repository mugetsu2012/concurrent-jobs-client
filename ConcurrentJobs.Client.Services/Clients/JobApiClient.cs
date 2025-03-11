using System.Net;
using System.Net.Http.Json;
using ConcurrentJobs.Client.Core.Clients;
using ConcurrentJobs.Client.Core;
using Microsoft.Extensions.Logging;

namespace ConcurrentJobs.Client.Services.Clients;

public class JobApiClient(HttpClient httpClient, ILogger<JobApiClient> logger) : IJobApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<JobApiClient> _logger = logger;

    public async Task<StartJobResponse> StartJobAsync(StartJobRequest request)
    {
        try
        {
            _logger.LogInformation("Sending request to start job of type {JobType} with name {JobName}",
                request.JobType, request.JobName);

            var response = await _httpClient.PostAsJsonAsync("jobs", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StartJobResponse>() 
                ?? throw new InvalidOperationException("Received null response from server");

            _logger.LogInformation("Successfully started job with ID {JobId}", result.JobId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while starting job");
            throw new ApplicationException($"Failed to start job: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while starting job");
            throw new ApplicationException($"An error occurred while starting job: {ex.Message}", ex);
        }
    }

    public async Task<JobStatusResponse> GetJobStatusAsync(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Requesting status for job {JobId}", jobId);

            var response = await _httpClient.GetAsync($"jobs/{jobId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Job {JobId} not found", jobId);
                throw new JobNotFoundException($"Job with ID {jobId} not found.");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JobStatusResponse>() 
                ?? throw new InvalidOperationException("Received null response from server");

            _logger.LogInformation("Received status {Status} for job {JobId}", result.Status, jobId);
            return result;
        }
        catch (JobNotFoundException)
        {
            // Rethrow job not found exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting job status for {JobId}", jobId);
            throw new ApplicationException($"Failed to get job status: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting job status for {JobId}", jobId);
            throw new ApplicationException($"An error occurred while getting job status: {ex.Message}", ex);
        }
    }

    public async Task<bool> CancelJobAsync(CancelJobRequest request)
    {
        try
        {
            _logger.LogInformation("Sending request to cancel job with ID {JobId}", request.JobId);

            var response = await _httpClient.PostAsJsonAsync("jobs/cancel", request);

            // Handle different response status codes
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully cancelled job with ID {JobId}", request.JobId);
                return true;
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Job {JobId} could not be cancelled: {ErrorMessage}",
                    request.JobId, errorContent);

                // Return false to indicate job was not cancelled (but this is not an error condition)
                return false;
            }
            else
            {
                response.EnsureSuccessStatusCode(); // This will throw for other non-success codes
                return false; // This line won't be reached but is needed for compilation
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while cancelling job {JobId}", request.JobId);
            throw new ApplicationException($"Failed to cancel job: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while cancelling job {JobId}", request.JobId);
            throw new ApplicationException($"An error occurred while cancelling job: {ex.Message}", ex);
        }
    }
}

// Custom exception for job not found scenarios
public class JobNotFoundException : Exception
{
    public JobNotFoundException(string message) : base(message) { }
    public JobNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}