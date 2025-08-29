using System.Text.Json;
using FlyingKit.Core.Metadata;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlyingKit.Core.Services;

public class JobHandler
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<JobHandler> _logger;

    public JobHandler(IConnectionMultiplexer connectionMultiplexer,ILogger<JobHandler> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }
    
    public async Task Handle(JobMetadata jobMetadata)
    {
        try
        {
            await jobMetadata.JobHandler();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Job Handler Error");
            var database = _connectionMultiplexer.GetDatabase();
            var jobKey = $"JOB:{jobMetadata.JobId}";
            var jobInfo = JsonSerializer.Deserialize<JobState>(database.StringGet(jobKey));
            jobInfo.MarkAsFailed(e.Message);
            database.StringSet($"JOB:{jobMetadata.JobId}",JsonSerializer.Serialize(jobInfo));
        }
    }
}