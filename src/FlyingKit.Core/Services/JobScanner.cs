using System.Text.Json;
using Coravel.Invocable;
using FlyingKit.Core.Abstractions;
using FlyingKit.Core.Metadata;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlyingKit.Core.Services;

public class JobScanner : IInvocable
{
    private readonly ILogger<JobScanner> _logger;
    private readonly JobProcessor _jobProcessor;
    private readonly IJobStorage _jobStorage;

    public JobScanner(
        ILogger<JobScanner> logger,
        JobProcessor jobProcessor,
        IJobStorage jobStorage)
    {
        _logger = logger;
        _jobProcessor = jobProcessor;
        _jobStorage = jobStorage;
    }


    public async Task Invoke()
    {
        _logger.LogInformation("Job Scanner Started");

        var pendingJobsIds = await _jobStorage.GetPendingJobsAsync();
        
        foreach (var pendingJobId in pendingJobsIds)
        {
           var pendingJob = await _jobStorage.GetJobAsync(pendingJobId);
           _logger.LogInformation("Processing Job {JobId}",pendingJob.JobId);
           await _jobProcessor.ProcessJobAsync(pendingJob);
        }
    }
}