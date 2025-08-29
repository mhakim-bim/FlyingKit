namespace FlyingKit.Core.Metadata;

public class JobMetadata
{
    public string JobId { get; }

    public Func<Task> JobHandler { get;  }
    
    public JobMetadata(string jobId, Func<Task> jobHandler)
    {
        JobId = jobId;
        JobHandler = jobHandler;
    }
}