using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kolokvijum1;

namespace ProcessingSystem.Tests
{
    public class ProcessingSystemTests
    {
        [Fact]
        public async Task Submit_ValidJob_ProcessesSuccessfullyAndReturnsResult()
        {
            // Arrange
            // Kreiramo sistem sa 2 worker niti i 10 mesta u redu
            var systemManager = new ProcessingSystemMenager(workerCount: 2, maxQueueSize: 10);
            var job = new Job
            {
                Type = JobType.IO,
                Payload = "delay:100", // Kratko kašnjenje od 100ms
                Priority = 1
            };

            // Act
            var handle = systemManager.Submit(job);
            int result = await handle.Result;

            // Assert
            Assert.InRange(result, 0, 100);

            // Zaustavljamo sistem da oslobodimo niti
            systemManager.StopSystem();
        }

        [Fact]
        public async Task Submit_JobTakingTooLong_ThrowsExceptionAfterRetries()
        {
            // Arrange
            var systemManager = new ProcessingSystemMenager(workerCount: 2, maxQueueSize: 10);
            var job = new Job
            {
                Type = JobType.IO,
                // Limit sistema je 2000ms. Ovaj posao traje 2500ms, pa će pasti 3 puta.
                Payload = "delay:2500",
                Priority = 1
            };

            // Act
            var handle = systemManager.Submit(job);

            // Assert
            // Await-ujemo rezultat i očekujemo da baci Exception koji sadrži reč "ABORT"
            var ex = await Assert.ThrowsAsync<Exception>(() => handle.Result);
            Assert.Contains("ABORT", ex.Message);

            systemManager.StopSystem();
        }
    }
}
