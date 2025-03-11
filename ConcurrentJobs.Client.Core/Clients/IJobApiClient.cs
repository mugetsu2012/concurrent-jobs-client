namespace ConcurrentJobs.Client.Core.Clients
{
    /// <summary>
    /// Client for interacting with the job API.
    /// </summary>
    public interface IJobApiClient
    {
        Task<StartJobResponse> StartJobAsync(StartJobRequest request);

        Task<JobStatusResponse> GetJobStatusAsync(Guid jobId);

        Task<bool> CancelJobAsync(CancelJobRequest request);
    }
}
