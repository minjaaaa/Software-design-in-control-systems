using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Kolokvijum1;

namespace Kolokvijum1
{
    // Pomoćna klasa za pamćenje statistike pre upisa u XML
    public class JobRecord
    {
        public JobType Type { get; set; }
        public bool IsSuccess { get; set; }
        public long DurationMs { get; set; }
    }

    public class ReportingService
    {
        // Koristimo Thread-Safe kolekciju jer više niti istovremeno završava poslove
        private readonly ConcurrentBag<JobRecord> _jobHistory = new ConcurrentBag<JobRecord>();
        private readonly string _logFilePath = "events_log.txt";
        private readonly string _xmlDirectory = "Reports";
        private readonly Timer _reportTimer;
        // "Saobraćajac" koji pušta samo jednu nit da piše u fajl
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

        public ReportingService(ProcessingSystemMenager systemManager)
        {
            Directory.CreateDirectory(_xmlDirectory);

            // 1. Pretplata na događaje koristeći LAMBDA izraze
            systemManager.JobCompletedEvent += async (job, result, duration) =>
            {
                _jobHistory.Add(new JobRecord { Type = job.Type, IsSuccess = true, DurationMs = duration });
                await LogToFileAsync("COMPLETED", job.Id, result.ToString());
            };

            systemManager.JobFailedEvent += async (job, message) =>
            {
                _jobHistory.Add(new JobRecord { Type = job.Type, IsSuccess = false, DurationMs = 0 });

                // Formatiramo status u zavisnosti da li je pao ili je potpuno abortiran
                string status = message.Contains("ABORT") ? "ABORTED" : "FAILED";
                await LogToFileAsync(status, job.Id, message);
            };

            // 2. Tajmer koji poziva metodu svako 1 minut (60000 ms)
            _reportTimer = new Timer(GenerateReport, null, 60000, 60000);
        }

        private async Task LogToFileAsync(string status, Guid jobId, string resultText)
        {
            // Asinhroni upis formata [DateTime] [Status] JobId, Result
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{status}] {jobId}, {resultText}\n";

            // Čekamo da fajl bude slobodan
            await _fileSemaphore.WaitAsync();
            try
            {
                // Sada kada smo sigurni da smo jedini, upisujemo u fajl
                await File.AppendAllTextAsync(_logFilePath, logLine);
            }
            finally
            {
                // OBAVEZNO oslobađamo semafor u finally bloku kako se fajl ne bi zauvek zaključao
                _fileSemaphore.Release();
            }
        }

        private void GenerateReport(object? state)
        {
            // 3. Generisanje izveštaja korišćenjem LINQ upita
            var historyCopy = _jobHistory.ToList();
            _jobHistory.Clear(); // Čistimo istoriju za sledeći minut

            if (!historyCopy.Any()) return;

            // Grupisanje po tipu i računanje statistike (broj uspešnih, neuspešnih i prosek vremena)
            var statistics = historyCopy
                .GroupBy(j => j.Type)
                .Select(g => new
                {
                    JobType = g.Key.ToString(),
                    CompletedCount = g.Count(j => j.IsSuccess),
                    FailedCount = g.Count(j => !j.IsSuccess),
                    AverageDuration = g.Where(j => j.IsSuccess).Select(j => j.DurationMs).DefaultIfEmpty(0).Average()
                })
                .OrderBy(s => s.JobType) // Sortirano po tipu, kao u zahtevu
                .ToList();

            // 4. Kreiranje XML fajla
            var xmlDocument = new XDocument(
                new XElement("Report", new XAttribute("Timestamp", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")),
                    statistics.Select(stat =>
                        new XElement("Statistics",
                            new XAttribute("Type", stat.JobType),
                            new XElement("ExecutedJobs", stat.CompletedCount),
                            new XElement("AverageExecutionTimeMs", Math.Round(stat.AverageDuration, 2)),
                            new XElement("FailedJobs", stat.FailedCount)
                        )
                    )
                )
            );

            SaveRollingXmlReport(xmlDocument);
        }

        private void SaveRollingXmlReport(XDocument doc)
        {
            string fileName = Path.Combine(_xmlDirectory, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            doc.Save(fileName);

            // 5. Održavanje maksimalno 10 fajlova izveštaja
            var allReports = new DirectoryInfo(_xmlDirectory)
                .GetFiles("Report_*.xml")
                .OrderBy(f => f.CreationTime)
                .ToList();

            if (allReports.Count > 10)
            {
                int filesToDelete = allReports.Count - 10;
                for (int i = 0; i < filesToDelete; i++)
                {
                    allReports[i].Delete();
                }
            }
        }
    }
}
