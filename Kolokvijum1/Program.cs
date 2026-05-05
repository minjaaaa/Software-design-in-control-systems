using Kolokvijum1;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessingSystem;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("--- Industrial Processing System ---");

            // 1. Učitavanje konfiguracije
            var config = SystemConfigLoader.Load("SystemConfig.xml");
            Console.WriteLine($"Konfiguracija ucitana. Worker niti: {config.WorkerCount}, Max red: {config.MaxQueueSize}");

            // 2. Inicijalizacija sistema
            var systemMenager = new ProcessingSystemMenager(config.WorkerCount, config.MaxQueueSize);

            // 3. Pokretanje servisa za izveštaje i logovanje (pretplaćivanje na događaje se vrši u konstruktoru)
            var reportingService = new ReportingService(systemMenager);

            // 4. Inicijalno učitavanje poslova iz XML-a
            foreach (var job in config.InitialJobs)
            {
                systemMenager.Submit(job);
            }
            Console.WriteLine($"Ucitano {config.InitialJobs.Count} pocetnih poslova iz XML-a.");

            // 5. Pokretanje producer niti koje nasumično dodaju poslove (dijagram prikazuje 5 niti)
            int producerCount = 5;
            for (int i = 0; i < producerCount; i++)
            {
                var producerThread = new Thread(() => ProducerLoop(systemMenager))
                {
                    IsBackground = true, // Gasi se automatski kada se ugasi Main program
                    Name = $"ProducerThread_{i}"
                };
                producerThread.Start();
            }

            Console.WriteLine("Sistem je pokrenut. Pritisnite ENTER za izlaz...\n");
            Console.ReadLine(); // Program ostaje upaljen dok korisnik ne pritisne Enter

            // Gašenje sistema
            systemMenager.StopSystem();
            Console.WriteLine("Sistem se gasi...");
        }
        catch (Exception ex)
        {
            // Globalni try-catch iz zahteva zadatka
            Console.WriteLine($"Kriticna greska u sistemu: {ex.Message}");
        }
    }

    // Metoda koju vrti svaka Producer nit
    private static void ProducerLoop(ProcessingSystemMenager systemManager)
    {
        var random = new Random();

        // Nasumični payload-ovi za testiranje
        string[] payloads = { "delay:500", "delay:2500", "numbers:1000,threads:2", "numbers:50000,threads:4" };

        while (true)
        {
            try
            {
                // Pauza između generisanja novih poslova (npr. od 1 do 3 sekunde)
                Thread.Sleep(random.Next(1000, 3000));

                var jobType = random.Next(100) > 50 ? JobType.IO : JobType.Prime;
                string payload = payloads[random.Next(payloads.Length)];

                var newJob = new Job
                {
                    Type = jobType,
                    Payload = payload,
                    Priority = random.Next(1, 10) // Nasumičan prioritet od 1 do 9
                };

                // Dodajemo u sistem
                systemManager.Submit(newJob);
            }
            catch (Exception ex)
            {
                // Sprečavamo da pad producer niti obori celu aplikaciju
                Console.WriteLine($"[Producer Error] {ex.Message}");
            }
        }
    }
}