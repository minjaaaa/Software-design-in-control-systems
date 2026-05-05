using System;
using System.Linq;
using Xunit;
using Kolokvijum1; 

namespace ProcessingSystem.Tests;

public class JobQueueTests
{
    [Fact]
    public void TryEnqueue_AddsSuccessfully_WhenSpaceAvailable()
    {
        // Arrange
        var queue = new JobQueue(maxQueueSize: 5);
        var job = new Job { Priority = 1 };

        // Act
        bool success = queue.TryEnqueue(job);

        // Assert
        Assert.True(success);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void TryEnqueue_RejectsJob_WhenQueueIsFull()
    {
        // Arrange
        var queue = new JobQueue(maxQueueSize: 2);
        queue.TryEnqueue(new Job { Priority = 1 });
        queue.TryEnqueue(new Job { Priority = 1 });

        // Attempting to add a third job
        var extraJob = new Job { Priority = 1 };

        // Act
        bool success = queue.TryEnqueue(extraJob);

        // Assert
        Assert.False(success); // We expect it to be rejected
        Assert.Equal(2, queue.Count); // Queue should still have only 2 items
    }

    [Fact]
    public void TryEnqueue_Idempotency_RejectsDuplicateJobId()
    {
        // Arrange
        var queue = new JobQueue(maxQueueSize: 5);
        var job = new Job { Id = Guid.NewGuid(), Priority = 1 };

        // Act
        queue.TryEnqueue(job); // First addition (successful)
        bool successSecondTime = queue.TryEnqueue(job); // Attempt to add the EXACT SAME job

        // Assert
        Assert.False(successSecondTime); // Must be rejected
        Assert.Equal(1, queue.Count); // Only one should be in the queue
    }

    [Fact]
    public void DequeueOrNull_ReturnsByPriority_LowerNumberIsHigherPriority()
    {
        // Arrange
        var queue = new JobQueue(maxQueueSize: 5);

        var lowPriorityJob = new Job { Priority = 5, Payload = "Low" };
        var highPriorityJob = new Job { Priority = 1, Payload = "High" };
        var mediumPriorityJob = new Job { Priority = 3, Payload = "Medium" };

        // Add them out of order
        queue.TryEnqueue(lowPriorityJob);
        queue.TryEnqueue(highPriorityJob);
        queue.TryEnqueue(mediumPriorityJob);

        // Act & Assert
        // The one with priority 1 must come out first
        var first = queue.DequeueOrNull();
        Assert.NotNull(first);
        Assert.Equal(1, first.Priority);
        Assert.Equal("High", first.Payload);

        // Then priority 3
        var second = queue.DequeueOrNull();
        Assert.Equal(3, second.Priority);

        // Then priority 5
        var third = queue.DequeueOrNull();
        Assert.Equal(5, third.Priority);

        // Then null because it is empty
        var fourth = queue.DequeueOrNull();
        Assert.Null(fourth);
    }

    [Fact]
    public void GetTopJobs_ReturnsCorrectNumberOfJobsSortedByPriority()
    {
        // Arrange
        var queue = new JobQueue(maxQueueSize: 10);
        queue.TryEnqueue(new Job { Priority = 10 });
        queue.TryEnqueue(new Job { Priority = 2 });
        queue.TryEnqueue(new Job { Priority = 5 });
        queue.TryEnqueue(new Job { Priority = 1 });

        // Act
        // Requesting the top 2 highest priority jobs (lowest numbers: 1 and 2)
        var top2 = queue.GetTopJobs(2).ToList();

        // Assert
        Assert.Equal(2, top2.Count);
        Assert.Equal(1, top2[0].Priority);
        Assert.Equal(2, top2[1].Priority);

        // Verify they are still in the queue (GetTopJobs only reads, doesn't remove)
        Assert.Equal(4, queue.Count);
    }
}