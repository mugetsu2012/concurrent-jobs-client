namespace ConcurrentJobs.Client.Core.Services
{
    public interface IJobService
    {
        /// <summary>
        /// Starts a new job with the specified type and name.
        /// </summary>
        /// <param name="jobType">Job type</param>
        /// <param name="jobName">Job name</param>
        /// <returns></returns>
        Task<Guid> StartJobAsync(string jobType, string jobName);

        /// <summary>
        /// Gets the status of a job by ID.
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <returns></returns>
        Task<JobStatusResponse> GetJobStatusAsync(Guid jobId);

        /// <summary>
        /// Cancels a job by ID.
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <returns></returns>
        Task<bool> CancelJobAsync(Guid jobId);

        /// <summary>
        /// Monitors the status of a job until it is no longer running.
        /// </summary>
        /// <param name="jobId">Job id to monitor</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task MonitorJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}
