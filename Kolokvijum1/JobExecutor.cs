using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public static class JobExecutor
    {
        // Glavna metoda koja usmerava posao na osnovu njegovog tipa
        public static async Task<int> ExecuteJobAsync(Job job)
        {
            if (job.Type == JobType.Prime)
            {
                var parameters = PayloadParser.ParsePrimePayload(job.Payload);
                // Prebacujemo CPU-intenzivan posao na ThreadPool kako ne bismo blokirali glavnu nit
                return await Task.Run(() => CalculatePrimesParallel(parameters.Limit, parameters.ThreadCount)); //salje se ThreadPool niti
            }
            else if (job.Type == JobType.IO)
            {
                int delayMs = PayloadParser.ParseIOPayload(job.Payload);
                // IO posao sa Thread.Sleep mora ići u Task.Run jer je Thread.Sleep sinhron i blokira nit
                return await Task.Run(() => ExecuteIoJob(delayMs));
            }

            throw new ArgumentException($"Unknown job type: {job.Type}");
        }

        // PRIME Processing 

        private static int CalculatePrimesParallel(int limit, int threadCount)
        {
            if (limit < 2) return 0;

            int totalPrimes = 0;

            // Ograničava maksimalan broj niti na onaj koji je prosleđen
            var options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };

            // Paralelno izvrsavanje
            Parallel.For(2, limit + 1, options,
                () => 0, // Inicijalizacija sume
                (i, loopState, localSum) =>
                {
                    if (IsPrime(i))
                    {
                        localSum++;
                    }
                    return localSum;
                },
                localSum => Interlocked.Add(ref totalPrimes, localSum) // Thread-safe dodavanje lokalne sume u globalnu
                //garantuje da ne moze vise niti istovremeno da menja totalPrimes
            );

            return totalPrimes;
        }

        private static bool IsPrime(int number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            var boundary = (int)Math.Floor(Math.Sqrt(number));

            for (int i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }

        //IO Obarda 

        private static int ExecuteIoJob(int delayMs)
        {
            
            Thread.Sleep(delayMs);

            // Vraca nasumican broj izmedju 0 i 100
            var random = new Random();
            return random.Next(0, 101); 
        }
    }
}
