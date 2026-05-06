using System;
using System.Collections.Generic;
using System.Linq;

namespace Kolokvijum1;

public class JobQueue
{
    // C# PriorityQueue radi tako sto manji broj znaci veci prioritet 
    // (npr. prioritet 1 ide pre prioriteta 3).
    private readonly PriorityQueue<Job, int> _queue = new PriorityQueue<Job, int>();

    // provera da li smo vec videli ovaj ID !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    private readonly HashSet<Guid> _seenJobs = new HashSet<Guid>();

  
    private readonly object _lockObj = new object();

    private readonly int _maxQueueSize;

    public JobQueue(int maxQueueSize)
    {
        _maxQueueSize = maxQueueSize;
    }

    // Dodavanje posla u red (Enqueue)
    public bool TryEnqueue(Job job)
    {
        lock (_lockObj) // Samo jedna nit moze da udje u ovaj blok u isto vreme
        {
            // provera da li vec postoji ovaj ID
            if (_seenJobs.Contains(job.Id))
            {
                return false; // posao je vec u sistemu ili je obradjen, odbijamo ga
            }

            // provera maksimalne velicine reda
            if (_queue.Count >= _maxQueueSize)
            {
                return false; // red je pun, odbijamo ga
            }

            // Sve provere su prosle, dodajemo posao
            _queue.Enqueue(job, job.Priority);
            _seenJobs.Add(job.Id); // Belezimo da smo ga registrovali

            return true;
        }
    }

    // Uzimanje posla sa najvecim prioritetom iz reda (Dequeue)
    public Job? DequeueOrNull()
    {
        lock (_lockObj)
        {
            if (_queue.Count > 0)
            {
                return _queue.Dequeue();
            }
            return null; // Red je prazan
        }
    }

    
    public IEnumerable<Job> GetTopJobs(int n)
    {
        lock (_lockObj)
        {
            // PriorityQueue nema laku metodu za citanje bez brisanja,
            // pa uzimamo nesortirane elemente, sortiramo ih rucno i vracamo prvih N.
            return _queue.UnorderedItems
                         .Select(item => item.Element)
                         .OrderBy(job => job.Priority)
                         .Take(n)
                         .ToList(); // ToList da bismo izvrsili upit unutar lock-a
        }
    }

    public int Count
    {
        get
        {
            lock (_lockObj)
            {
                return _queue.Count;
            }
        }
    }
    public Job? GetJob(Guid id)
    {
        // Obavezno koristimo lock da bi pretraga bila bezbedna (thread-safe)
        lock (_lockObj){
            
                // Pristupamo elementima preko UnorderedItems, 
                // trazimo onaj čiji se ID poklapa i vraćamo njegov Element
                var foundItem = _queue.UnorderedItems.FirstOrDefault(item => item.Element.Id == id);
                return foundItem.Element;
        }
    }
}	

