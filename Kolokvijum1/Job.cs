using System;

public enum JobType
{
    Prime,
    IO
}

public class Job
{
    public Guid Id { get; set; }
    public JobType Type { get; set; }
    public string Payload { get; set; }
    public int Priority { get; set; }

    public Job()
    {
        Id = Guid.NewGuid();
    }
    
}
