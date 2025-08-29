using FlyingKit.Core.Metadata;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace FlyingKit.Core.Services;

/// <summary>
/// is responsible for processing jobs and handling retries and failures
/// </summary>
public class JobProcessor
{
    private readonly JobHandlersStorage _jobHandlersStorage;
    
    private readonly RedisJobStorage _redisJobStorage;
    private readonly ILogger<JobProcessor> _logger;

    public JobProcessor(JobHandlersStorage jobHandlersStorage, RedisJobStorage redisJobStorage,ILogger<JobProcessor> logger)
    {
        _jobHandlersStorage = jobHandlersStorage;
        _redisJobStorage = redisJobStorage;
        _logger = logger;
        
    }

    public async Task ProcessJobAsync(JobState jobState)
    {
        if (jobState.ShouldRetry())
        {

           var pipeline =  Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, 
                    (retry) => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                    onRetryAsync: async (exception, timeSpan,retryCount, context) =>
                {
                    _logger.LogInformation("Retrying");
                 
                    if(!context.TryGetValue("JobState",out object jobState))
                        return;
                    
                    var newJobState = (JobState)jobState;
                    
                    newJobState.Status = JobStatus.Retrying;

                    newJobState.RetryCount = retryCount;
                    newJobState.NextRetryAt = DateTime.UtcNow.Add(timeSpan);
                    newJobState.LastError = exception.Message;
                    newJobState.ProcessingStartedAt = DateTime.UtcNow;
                    
                    await _redisJobStorage.UpdateJobStatusAsync(newJobState);

                });
          
           var context = new Context(jobState.JobId,new Dictionary<string, object>()
           {
               {"JobState",jobState}
           });
            
            jobState = await pipeline.ExecuteAsync(async (c,ct) =>
            {
                var job = _jobHandlersStorage.Get(jobState.JobType); 
                await job.HandleAsync(jobState.Arguments);
                jobState.Status = JobStatus.Completed;
                return jobState;
            },context,CancellationToken.None);
            
            await _redisJobStorage.RemoveJobAsync(jobState.JobId);
        }
        
    }
    
    private async ValueTask OnRetry(OnRetryArguments<JobState> arg)
    {
        _logger.LogInformation("Retrying");
    }
}