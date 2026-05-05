using System;

public class JobTests
{
    [Fact]
    public void Konstruktor_KreiranjemPosla_AutomatskiDodeljujeJedinstveniGuid()
    {
        // Act
        var job1 = new Job();
        var job2 = new Job();

        // Assert
        Assert.NotEqual(Guid.Empty, job1.Id);
        Assert.NotEqual(Guid.Empty, job2.Id);
        Assert.NotEqual(job1.Id, job2.Id);
    }
}
