namespace FlyingKit.Core.Metadata;

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Scheduled,
    Retrying
}

public class JobState
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(15);
    public DateTime? NextRetryAt { get; set; }
    public string LastError { get; set; }
  
    public string JobType { get; set; }
    public object Arguments { get; set; }
    
    public void MarkAsProcessing()
    {
        Status = JobStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = JobStatus.Failed;
        LastError = errorMessage;
    }

    public bool ShouldRetry()
    {
        return RetryCount < MaxRetries;
    }

    public void ScheduleRetry()
    {
        RetryCount++;
        Status = JobStatus.Scheduled;
        NextRetryAt = DateTime.UtcNow.Add(RetryDelay);
    }
    
    public static JobState Create(string jobType)
    {
        return new JobState
        {
            JobType = jobType ?? Guid.NewGuid().ToString()
        };
    }
    
    public static JobState Create(string jobType,object arguments)
    {
        return new JobState
        {
            JobType = jobType ?? Guid.NewGuid().ToString(),
            Arguments = arguments
        };
    }
    
  
}