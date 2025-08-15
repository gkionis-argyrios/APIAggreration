using APIAggreration.Models;
using System.Collections.Concurrent;

namespace APIAggreration.Classes
{
    public static class PerformanceTracker
    {
        private static readonly ConcurrentDictionary<string, ApiPerformanceStats> _stats = new();

        public static void Record(string apiName, double elapsedMs)
        {
            var stats = _stats.GetOrAdd(apiName, _ => new ApiPerformanceStats());

            lock (stats) // small lock to ensure safe updates
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
