# Industrial Processing System ⚙️

Ovaj projekat predstavlja robusni, višenitni (multi-threaded) sistem za obradu industrijskih poslova, razvijen u jeziku **C# (.NET)**. Sistem simulira red čekanja (Queue) sa prioritetima, paralelno izvršavanje zadataka i automatsko generisanje izveštaja, uz punu otpornost na greške (Fault Tolerance).

Projekat je realizovan kao predispitna obaveza (Kolokvijum 1).

## 🚀 Glavne funkcionalnosti

*   **Višenitna arhitektura (Multi-threading):** Sistem koristi 5 *Producer* niti koje asinhrono generišu poslove i definisan broj *Worker* niti koje ih paralelno obrađuju.
*   **Thread-Safe Priority Queue:** Prilagođena implementacija reda čekanja koja koristi `lock` mehanizme kako bi se osiguralo bezbedno dodavanje i preuzimanje poslova na osnovu njihovog prioriteta.
*   **Otpornost na greške (Timeout & Retry):** 
    *   Implementiran *Timeout* mehanizam (korišćenjem `Task.WhenAny`) koji prekida poslove ako traju duže od 2 sekunde.
    *   Ugrađen *Retry* mehanizam koji automatski ponavlja neuspešan posao do 3 puta pre nego što ga proglasi prekinutim (`ABORTED`).
*   **Asinhrono I/O logovanje:** Svi događaji se bezbedno upisuju u `events_log.txt` korišćenjem `SemaphoreSlim` klase za prevenciju sudaranja niti (Thread-safety).
*   **Napredno izveštavanje (LINQ & XML):** Na svakih 60 sekundi, sistem koristi LINQ za grupisanje, prebrojavanje i računanje prosečnog vremena izvršavanja poslova po tipu. Rezultati se čuvaju kao XML fajlovi uz *Rolling logs* mehanizam (čuva se samo poslednjih 10 izveštaja).

## 🛠️ Tehnologije
*   **Jezik:** C# 12 / .NET 8
*   **Testiranje:** xUnit framework
*   **Obrada podataka:** LINQ to Objects, LINQ to XML (`XDocument`)
*   **Konkurentnost:** `Thread`, `Task`, `SemaphoreSlim`, `ConcurrentBag`, `ConcurrentDictionary`

## ⚙️ Konfiguracija sistema
Sistem se inicijalizuje učitavanjem `SystemConfig.xml` fajla koji definiše broj *Worker* niti, maksimalnu veličinu reda i početne poslove. Primer konfiguracije:

```xml
<SystemConfig>
  <WorkerCount>5</WorkerCount>
  <MaxQueueSize>100</MaxQueueSize>
  <Jobs>
    <Job Payload="delay:500" Priority="1" Type="IO"/>
    <Job Payload="numbers:1000,threads:2" Priority="5" Type="Prime"/>
  </Jobs>
</SystemConfig>