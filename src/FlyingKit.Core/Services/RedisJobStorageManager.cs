using System.Text.Json;
using FlyingKit.Core.Metadata;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlyingKit.Core.Services;

public class RedisJobStorage
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisJobStorage> _logger;
    private const string JobKeyPrefix = "job:";
    private const string PendingQueue = "jobs:pending";
    private const string ProcessingSet = "jobs:processing";
    private const string ScheduledSet = "jobs:scheduled";
    private const string FailedSet = "jobs:failed";
    private const string CompletedSet = "jobs:completed";

    public RedisJobStorage(IConnectionMultiplexer redis, ILogger<RedisJobStorage> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string> StoreJobAsync(JobState jobState)
    {
        var db = _redis.GetDatabase();
        var jobKey = JobKeyPrefix + jobState.JobId;
        var serialized = JsonSerializer.Serialize(jobState);
        
        await db.StringSetAsync(jobKey, serialized);
        
        // Add to appropriate queue/set based on status
        switch (jobState.Status)
        {
            case JobStatus.Pending:
                await db.ListRightPushAsync(PendingQueue, jobState.JobId);
                break;
            case JobStatus.Scheduled:
                await db.SortedSetAddAsync(ScheduledSet, jobState.JobId, 
                    ((DateTimeOffset)jobState.NextRetryAt).ToUnixTimeSeconds());
                break;
        }
        
        _logger.LogInformation($"Stored job {jobState.JobId} with status {jobState.Status}");
        return jobState.JobId;
    }

    public async Task<JobState> GetJobAsync(string jobId)
    {
        var db = _redis.GetDatabase();
        var jobKey = JobKeyPrefix + jobId;
        var serialized = await db.StringGetAsync(jobKey);
        
        if (serialized.IsNullOrEmpty)
            return null;
            
        return JsonSerializer.Deserialize<JobState>(serialized);
    }
    
    public async Task UpdateJobStatusAsync(JobState jobState)
    {
        var db = _redis.GetDatabase();
        var jobKey = JobKeyPrefix + jobState.JobId;
        
        // Update the job state
        var serialized = JsonSerializer.Serialize(jobState);
        await db.StringSetAsync(jobKey, serialized);
        
        // Move between queues/sets based on new status
        switch (jobState.Status)
        {
            case JobStatus.Processing:
                await db.ListRemoveAsync(PendingQueue, jobState.JobId);
                await db.SetAddAsync(ProcessingSet, jobState.JobId);
                break;
                
            case JobStatus.Completed:
                await db.SetRemoveAsync(ProcessingSet, jobState.JobId);
                await db.SetAddAsync(CompletedSet, jobState.JobId);
                break;
                
            case JobStatus.Failed:
                await db.SetRemoveAsync(ProcessingSet, jobState.JobId);
                await db.SetAddAsync(FailedSet, jobState.JobId);
                break;
                
            case JobStatus.Scheduled:
                await db.SetRemoveAsync(ProcessingSet, jobState.JobId);
                await db.SortedSetAddAsync(ScheduledSet, jobState.JobId, 
                    ((DateTimeOffset)jobState.NextRetryAt).ToUnixTimeSeconds());
                break;
        }
    }
    
    
    
    public async Task<string> DequeueJobAsync()
    {
        var db = _redis.GetDatabase();
        
        // First try to get a pending job
        var jobId = await db.ListLeftPopAsync(PendingQueue);
        if (!jobId.IsNullOrEmpty)
            return jobId;
            
        // Then check for scheduled jobs that are due
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var scheduledJobs = await db.SortedSetRangeByScoreAsync(ScheduledSet, 0, now, take: 1);
        
        if (scheduledJobs.Length > 0)
        {
            var scheduledJobId = scheduledJobs[0];
            await db.SortedSetRemoveAsync(ScheduledSet, scheduledJobId);
            return scheduledJobId;
        }
        
        return null;
    }
    
    public async Task<List<string>> GetProcessingJobsAsync()
    {
        var db = _redis.GetDatabase();
        var jobIds = await db.SetMembersAsync(ProcessingSet);
        return jobIds.Select(id => (string)id).ToList();
    }
    
    public async Task RemoveJobAsync(string jobId)
    {
        var db = _redis.GetDatabase();
        var jobKey = JobKeyPrefix + jobId;
        
        await db.KeyDeleteAsync(jobKey);
        await db.ListRemoveAsync(PendingQueue, jobId);
        await db.SetRemoveAsync(ProcessingSet, jobId);
        await db.SetRemoveAsync(FailedSet, jobId);
        await db.SetRemoveAsync(CompletedSet, jobId);
        await db.SortedSetRemoveAsync(ScheduledSet, jobId);
    }
}