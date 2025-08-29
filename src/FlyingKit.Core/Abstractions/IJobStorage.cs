using FlyingKit.Core.Metadata;

namespace FlyingKit.Core.Abstractions;

public interface IJobStorage
{
    Task<string> StoreJobAsync(JobState jobState);
    Task<JobState> GetJobAsync(string jobId);
    Task UpdateJobStatusAsync(JobState jobState);
    Task<string> DequeueJobAsync();
    Task<List<string>> GetProcessingJobsAsync();
    Task RemoveJobAsync(string jobId);

    Task<List<string>> GetPendingJobsAsync();
}