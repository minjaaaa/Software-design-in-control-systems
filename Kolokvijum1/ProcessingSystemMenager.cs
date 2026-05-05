using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public class ProcessingSystemMenager
    {
        private readonly JobQueue _jobQueue;
        private readonly int _workerCount;

        // Čuva reference na poslove koji su trenutno u obradi kako bismo im prosledili rezultat
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _activeJobs;

        // Služi za bezbedno zaustavljanje worker niti kada ugasimo program
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Događaji na koje ćemo se pretplatiti 
        public event Action<Job, int, long>? JobCompletedEvent;
        public event Action<Job, string>? JobFailedEvent;

        public ProcessingSystemMenager(int workerCount, int maxQueueSize)
        {
            _workerCount = workerCount;
            _jobQueue = new JobQueue(maxQueueSize);
            _activeJobs = new ConcurrentDictionary<Guid, TaskCompletionSource<int>>();
            _cancellationTokenSource = new CancellationTokenSource();

            StartWorkerThreads();
        }

        private void StartWorkerThreads()
        {
            // Kreiramo i pokrećemo traženi broj worker niti
            for (int i = 0; i < _workerCount; i++)
            {
                var workerThread = new Thread(WorkerLoop)
                {
                    IsBackground = true, // Gasi se automatski kada se ugasi Main program
                    Name = $"WorkerThread_{i}"
                };
                workerThread.Start();
            }
        }

        // Prima posao i vraća JobHandle odmah
        public JobHandle Submit(Job job)
        {
            // TaskCreationOptions.RunContinuationsAsynchronously sprečava mrtve petlje (deadlocks)
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handle = new JobHandle { Id = job.Id, Result = tcs.Task };

            // Pokušavamo da ubacimo posao u red
            if (!_jobQueue.TryEnqueue(job))
            {
                // Red je pun ili je ID duplikat 
                tcs.SetException(new InvalidOperationException("Red je pun ili je posao duplikat."));
                return handle;
            }

            // Ako je uspešno dodat, pamtimo njegov TaskCompletionSource
            _activeJobs.TryAdd(job.Id, tcs);

            return handle;
        }

        
        private void WorkerLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Nit pokušava da uzme posao sa najvećim prioritetom
                var job = _jobQueue.DequeueOrNull();

                if (job != null)
                {
                    // Ako je našla posao, obrađuje ga. 
                    // Koristimo .GetAwaiter().GetResult() da bi ova worker nit ostala
                    // rezervisana za ovaj posao dok se on ne završi (sa svim retry pokušajima).
                    ProcessJobWithRetryAsync(job).GetAwaiter().GetResult();
                }
                else
                {
                    // Ako je red prazan, nit spava 100ms 
                    Thread.Sleep(100);
                }
            }
        }

        // Asinhrona metoda za obradu s
        private async Task ProcessJobWithRetryAsync(Job job)
        {
            int maxAttempts = 3; // 1 originalni pokušaj + 2 retry pokušaja
            TimeSpan timeoutLimit = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var executionTask = JobExecutor.ExecuteJobAsync(job);
                    // Pokrećemo izvršavanje posla iz našeg JobExecutor-a

                    // Kreiramo zadatak koji simulira otkucavanje tajmera od 2 sekunde
                    var timeoutTask = Task.Delay(timeoutLimit);

                    // Task.WhenAny se završava čim se jedan od ova dva taska završi prvi
                    var firstCompletedTask = await Task.WhenAny(executionTask, timeoutTask);

                    if (firstCompletedTask == timeoutTask)
                    {
                        // Timeout task se završio prvi, što znači da obrada predugo traje
                        throw new TimeoutException($"Posao traje duže od 2 sekunde.");
                    }

                    // Ako smo ovde, obrada je uspela na vreme!
                    int result = await executionTask;
                    stopwatch.Stop(); // Zaustavi štopericu kada se posao završi
                    // Okidamo događaj za uspešan posao
                    
                    JobCompletedEvent?.Invoke(job, result, stopwatch.ElapsedMilliseconds);

                    // Upisujemo rezultat nazad klijentu
                    if (_activeJobs.TryRemove(job.Id, out var tcs))
                    {
                        tcs.SetResult(result);
                    }

                    return; 
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts)
                    {
                        // Ovo je bio treći (poslednji) pokušaj
                        string abortMessage = $"ABORT";
                        JobFailedEvent?.Invoke(job, abortMessage);

                        if (_activeJobs.TryRemove(job.Id, out var tcs))
                        {
                            // Klijent koji čeka na await handle.Result će dobiti ovu grešku
                            tcs.SetException(new Exception($"{abortMessage} - {ex.Message}"));
                        }
                    }
                    else
                    {
                        // Posao je pao, ali imamo još pokušaja. Okidamo događaj da zabeležimo pad u fajl.
                        JobFailedEvent?.Invoke(job, $"FAIL (Attempt {attempt})");
                    }
                }
            }
        }

        
        public IEnumerable<Job> GetTopJobs(int n)
        {
            return _jobQueue.GetTopJobs(n);
        }

        public void StopSystem()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
