using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kolokvijum1
{
    // Klasa koja predstavlja podatke iz konfiguracije
    public class SystemConfiguration
    {
        public int WorkerCount { get; set; }
        public int MaxQueueSize { get; set; }
        public List<Job> InitialJobs { get; set; } = new List<Job>();
    }

    public class SystemConfigLoader
    {
        public static SystemConfiguration Load(string filePath)
        {
            var config = new SystemConfiguration();

            // Koristimo LINQ to XML za parsiranje
            var doc = XDocument.Load(filePath);
            var root = doc.Element("SystemConfig");

            config.WorkerCount = int.Parse(root!.Element("WorkerCount")!.Value);
            config.MaxQueueSize = int.Parse(root!.Element("MaxQueueSize")!.Value);

            // Parsiranje inicijalnih poslova
            foreach (var jobNode in root!.Element("Jobs")!.Elements("Job"))
            {
                var job = new Job
                {
                    Type = Enum.Parse<JobType>(jobNode.Attribute("Type")!.Value),
                    Payload = jobNode.Attribute("Payload")!.Value,
                    Priority = int.Parse(jobNode.Attribute("Priority")!.Value)
                };
                config.InitialJobs.Add(job);
            }

            return config;
        }
    }
}
