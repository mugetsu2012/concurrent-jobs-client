namespace ConcurrentJobs.Client.Core
{
    public record StartJobResponse(Guid JobId);
    public record JobStatusResponse(Guid JobId, string JobType, string JobName, string Status);
}
