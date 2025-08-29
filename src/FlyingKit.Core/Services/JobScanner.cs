using System.Text.Json;
using Coravel.Invocable;
using FlyingKit.Core.Metadata;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlyingKit.Core.Services;

public class JobScanner : IInvocable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<JobScanner> _logger;
    private readonly JobProcessor _jobProcessor;

    public JobScanner(IConnectionMultiplexer connectionMultiplexer,
        ILogger<JobScanner> logger,
         JobProcessor jobProcessor)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _jobProcessor = jobProcessor;
    }


    public async Task Invoke()
    {
        _logger.LogInformation("Job Scanner Started");
        
        var database = _connectionMultiplexer.GetDatabase();
        
        foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            if (!server.IsConnected) continue;

            // Enumerate lazily via SCAN; avoid materializing unless necessary
            foreach (var key in server.Keys(database: database.Database,
                         pattern: "job:*", pageSize: 10))
            {
                _logger.LogInformation($"Found unfinished job {key}");

                var redisValue = await _connectionMultiplexer.GetDatabase().StringGetAsync(key);
                
                _logger.LogInformation($"Job State {redisValue}");

                var jobState = JsonSerializer.Deserialize<JobState>(redisValue);

                await _jobProcessor.ProcessJobAsync(jobState);
                
            }
        }
    }
}