using ConcurrentJobs.Client.Core;
using ConcurrentJobs.Client.Core.Clients;
using ConcurrentJobs.Client.Core.Services;
using Microsoft.Extensions.Logging;

namespace ConcurrentJobs.Client.Services
{
    public class JobService(IJobApiClient apiClient, ILogger<JobService> logger) : IJobService
    {
        private readonly IJobApiClient _apiClient = apiClient;
        private readonly ILogger<JobService> _logger = logger;

        public async Task<Guid> StartJobAsync(string jobType, string jobName)
        {
            var request = new StartJobRequest(jobType, jobName);
            var response = await _apiClient.StartJobAsync(request);
            return response.JobId;
        }
        
        public async Task<JobStatusResponse> GetJobStatusAsync(Guid jobId)
        {
            return await _apiClient.GetJobStatusAsync(jobId);
        }

        public async Task<bool> CancelJobAsync(Guid jobId)
        {
            var request = new CancelJobRequest(jobId);
            return await _apiClient.CancelJobAsync(request);
        }
        
        public async Task MonitorJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Monitoring job {JobId}...", jobId);

            // Keep checking status until job is no longer running or operation is cancelled
            bool isRunning = true;
            while (isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var status = await GetJobStatusAsync(jobId);

                    if (status.Status != "Running")
                    {
                        isRunning = false;
                        _logger.LogInformation("Job {JobId} has finished with status: {Status}", jobId, status.Status);
                    }
                    else
                    {
                        _logger.LogInformation("Job {JobId} is still running...", jobId);
                        await Task.Delay(1000, cancellationToken); // Poll every second
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while monitoring job {JobId}", jobId);
                    throw;
                }
            }
        }
    }
}
