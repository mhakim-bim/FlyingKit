using FlyingKit.Core.Abstractions;
using FlyingKit.Core.Metadata;
using Microsoft.Extensions.Logging;

namespace FlyingKit.Core.Services;

public class JobScheduler
{
    private readonly RedisJobStorage _jobStorage;
    
    private readonly ILogger<JobScheduler> _logger;
    private readonly JobHandlersStorage _jobHandlersStorage;

    public JobScheduler(RedisJobStorage jobStorage, ILogger<JobScheduler> logger,JobHandlersStorage jobHandlersStorage)
    {
        _jobStorage = jobStorage;
        _logger = logger;
        _jobHandlersStorage = jobHandlersStorage;
    }

    public async Task<string> EnqueueAsync<T>(T tickJob) where T : ITickJob
    {
        var jobType = tickJob.GetType().ToString();
        
        _logger.LogInformation($"Enqueueing job {jobType}");
        
        _jobHandlersStorage.Add(jobType,tickJob);

        var jobState = JobState.Create(jobType);
        
        return await _jobStorage.StoreJobAsync(jobState);
    }
    
    
    public async Task<string> EnqueueAsync<T>(T tickJob,object arguments) where T : ITickJob
    {
        var jobType = tickJob.GetType().ToString();
        
        _logger.LogInformation($"Enqueueing job {jobType}");
        
        _jobHandlersStorage.Add(jobType,tickJob);

        var jobState = JobState.Create(jobType,arguments);
        
        return await _jobStorage.StoreJobAsync(jobState);
    }
    
    public async Task<bool> DeleteAsync(string jobId)
    {
        await _jobStorage.RemoveJobAsync(jobId);
        return true;
    }
    
    public async Task<JobState> GetJobAsync(string jobId)
    {
        return await _jobStorage.GetJobAsync(jobId);
    }

}