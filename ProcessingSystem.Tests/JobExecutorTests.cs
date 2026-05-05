using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kolokvijum1;

namespace ProcessingSystem.Tests
{
    public class JobExecutorTests
    {
        [Fact]
        public async Task ExecuteJobAsync_PrimeType_CalculatesCorrectNumberOfPrimes()
        {
            // Arrange
            var job = new Job
            {
                Type = JobType.Prime,
                // Format iz XML-a: prosti brojevi do 100, koristeći 2 niti
                Payload = "numbers:100,threads:2"
            };

            // Act
            int result = await JobExecutor.ExecuteJobAsync(job);

            // Assert
            Assert.Equal(25, result);
        }

        [Fact]
        public async Task ExecuteJobAsync_IoType_ReturnsNumberBetweenZeroAndOneHundred()
        {
            // Arrange
            var job = new Job
            {
                Type = JobType.IO,
                // Format iz XML-a: kratko kašnjenje od 10ms da test ne traje dugo
                Payload = "delay:10"
            };

            // Act
            int result = await JobExecutor.ExecuteJobAsync(job);

            // Assert
            // Rezultat mora biti u opsegu [0, 100]
            Assert.InRange(result, 0, 100);
        }

        [Fact]
        public async Task ExecuteJobAsync_InvalidPayload_ThrowsArgumentException()
        {
            // Arrange
            var job = new Job
            {
                Type = JobType.Prime,
                Payload = "bad_payload" // Neispravan format
            };

            // Act & Assert
            // Očekujemo da će parsiranje unutar ExecuteJobAsync baciti izuzetak
            await Assert.ThrowsAsync<ArgumentException>(() => JobExecutor.ExecuteJobAsync(job));
        }
    }
}
