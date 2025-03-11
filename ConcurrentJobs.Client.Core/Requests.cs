namespace ConcurrentJobs.Client.Core
{
    public record StartJobRequest(string JobType, string JobName);
    public record CancelJobRequest(Guid JobId);
}