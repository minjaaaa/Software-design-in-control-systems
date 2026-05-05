using System;
using Xunit;
using Kolokvijum1;
public class PayloadParserTests
{
    // --- Testovi za Prime Payload ---

    [Fact]
    public void ParsePrimePayload_ValidFormat_ReturnsCorrectParameters()
    {
        // Arrange & Act
        var result = PayloadParser.ParsePrimePayload("numbers:10_000,threads:4");

        // Assert
        Assert.Equal(10000, result.Limit);
        Assert.Equal(4, result.ThreadCount);
    }

    [Fact]
    public void ParsePrimePayload_ThreadsLessThanOne_ClampsToOne()
    {
        // Act
        var result = PayloadParser.ParsePrimePayload("numbers:500,threads:-2");

        // Assert
        Assert.Equal(1, result.ThreadCount);
    }

    [Fact]
    public void ParsePrimePayload_ThreadsGreaterThanEight_ClampsToEight()
    {
        // Act
        var result = PayloadParser.ParsePrimePayload("numbers:500,threads:15");

        // Assert
        Assert.Equal(8, result.ThreadCount); // Proveravamo da li je primenjeno ograničenje [1, 8]
    }

    [Fact]
    public void ParsePrimePayload_InvalidFormat_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PayloadParser.ParsePrimePayload("just_text"));
        Assert.Throws<ArgumentException>(() => PayloadParser.ParsePrimePayload("numbers:1000,wrong:4"));
    }

    // --- Testovi za IO Payload ---

    [Fact]
    public void ParseIOPayload_ValidFormat_ReturnsValue()
    {
        // Act
        int delay = PayloadParser.ParseIOPayload("delay:1_500");

        // Assert
        Assert.Equal(1500, delay);
    }

    [Fact]
    public void ParseIOPayload_InvalidFormat_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PayloadParser.ParseIOPayload("wait:500"));
    }
}
