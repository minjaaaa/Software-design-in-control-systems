using System;

// pomocna klasa za strukturisanje Prime parametara
public class PrimeParameters
{
    public int Limit { get; set; }
    public int ThreadCount { get; set; }
}

public static class PayloadParser
{
    public static PrimeParameters ParsePrimePayload(string payload)
    {
        
        string cleanPayload = payload.Replace("_", "").ToLower();

        // Očekivani format: "numbers:10000,threads:3"
        var parts = cleanPayload.Split(',');

        if (parts.Length != 2)
            throw new ArgumentException("Invalid Prime payload format.");

        var numbersPart = parts[0].Split(':'); // niz [numbers] [10000]
        var threadsPart = parts[1].Split(':'); // niz [threads] [3]

        if (numbersPart.Length != 2 || numbersPart[0].Trim() != "numbers" ||
            threadsPart.Length != 2 || threadsPart[0].Trim() != "threads")
        {
            throw new ArgumentException("Invalid keys in Prime payload.");
        }

        int limit = int.Parse(numbersPart[1].Trim());
        int threadCount = int.Parse(threadsPart[1].Trim());

        // Ograničavanje broja niti na interval [1, 8]
        if (threadCount < 1) threadCount = 1;
        if (threadCount > 8) threadCount = 8;

        return new PrimeParameters { Limit = limit, ThreadCount = threadCount };
    }

    public static int ParseIOPayload(string payload)
    {
        string cleanPayload = payload.Replace("_", "").ToLower();

        // Očekivani format: "delay:1000"
        var parts = cleanPayload.Split(':');

        if (parts.Length != 2 || parts[0].Trim() != "delay")
            throw new ArgumentException("Invalid IO payload format.");

        return int.Parse(parts[1].Trim());
    }
}