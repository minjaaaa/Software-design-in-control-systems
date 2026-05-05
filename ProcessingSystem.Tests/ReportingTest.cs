using Kolokvijum1;
using ProcessingSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProcessingSystem.Tests
{
    public class ReportingTest
    {
        [Fact]
        public async Task ReportingService_LogsCompletedJob_ToFile()
        {
            // Arrange
            var manager = new ProcessingSystemMenager(1, 10);
            var service = new ReportingService(manager);
            var job = new Job { Id = Guid.NewGuid(), Type = JobType.IO, Payload = "delay:10" };
            string logFile = "events_log.txt";

            // Ako fajl postoji, obrisemo ga da budemo sigurni u test
            if (File.Exists(logFile)) File.Delete(logFile);

            // Act
            manager.Submit(job);

            // Sac ekamo malo da worker nit obradi posao i okine dogadjaj
            await Task.Delay(500);

            // Assert
            Assert.True(File.Exists(logFile));
            string content = File.ReadAllText(logFile);
            Assert.Contains(job.Id.ToString(), content);

            manager.StopSystem();
        }
    }
}
