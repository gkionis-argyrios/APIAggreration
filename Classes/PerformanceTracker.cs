using APIAggreration.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Timers;

namespace APIAggreration.Classes
{
    public static class PerformanceTracker
    {
        private static readonly ConcurrentDictionary<string, ApiPerformanceStats> _stats = new();
        //it’s thread-safe → multiple threads can read/write without corrupting data.

        //ConcurrentDictionary is only responsible for safely adding, removing, and retrieving
        //items from the dictionary itself.
        //_stats.GetOrAdd("News", ...) will never corrupt the dictionary,
        // if > 1 threads call it at the same time.

        public static void Record(string apiName, double elapsedMs)
        {
            var stats = _stats.GetOrAdd(apiName, _ => new ApiPerformanceStats());
            //_=> provide factory create new value if does not exist add to dict and return it

            //lock (stats) ensures only one thread at a time can enter this block for the same stats object.
            //So when one thread updates "News" stats, others must wait until it’s finished.
            //This guarantees the counters stay correct.

            lock (stats) // thread safe lock per api prevent race condition
            {
                stats.TotalRequests++;
                stats.TotalResponseTimeMs += elapsedMs;

                if (elapsedMs < 100)
                    stats.FastCount++;
                else if (elapsedMs <= 200)
                    stats.AverageCount++;
                else
                    stats.SlowCount++;
            }
        }
        public static Dictionary<string, ApiPerformanceStats> GetStats()
        {
            return _stats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
