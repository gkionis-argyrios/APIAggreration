using APIAggreration.Classes;
using APIAggreration.Interfaces;
using APIAggreration.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using static APIAggreration.Models.DataProviderModel;

namespace APIAggreration.Services
{
    public class DataProviderService : IDataProviderService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        public DataProviderService(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;
        }

        private async Task<List<AggregatedItemModel>> GetCachedOrGetFromNewsAsync(
            string cacheKey,
            Task<List<AggregatedItemModel>> fetchFunc,
            TimeSpan duration)
        {
            if (_cache.TryGetValue(cacheKey, out List<AggregatedItemModel>? cached))
            {
                return cached;
            }

            var data = await fetchFunc;
            if (data != null && data.Count > 0) _cache.Set(cacheKey, data, duration);
            return data;
        }

        private async Task<List<AggregatedItemModel>> GetCachedOrGetFromWeatherAsync(
            string cacheKey,
            Task<List<AggregatedItemModel>> fetchFunc,
            TimeSpan duration)
        {
            if (_cache.TryGetValue(cacheKey, out List<AggregatedItemModel>? cached))
            {
                return cached;
            }

            var data = await fetchFunc;
            if (data != null && data.Count > 0) _cache.Set(cacheKey, data, duration);
            return data;
        }

        private async Task<List<AggregatedItemModel>> GetFromNewsAsync()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var json = await _http.GetStringAsync("https://newsapi.org/v2/top-headlines?sources=bbc-news&apiKey=02be176b87ed479b885fce22a14eea79");

                sw.Stop();
                PerformanceTracker.Record("News", sw.Elapsed.TotalMilliseconds);

                var rawItems = System.Text.Json.JsonSerializer.Deserialize<List<Models.NewsResponse>>(json);

                if (rawItems == null) return [];
                return rawItems.Select(x => new AggregatedItemModel
                {
                    Date = x.Date,
                   // Category = x.Category,
                    Source = "News"
                }).ToList();
            }
            catch (Exception ex)
            {
                sw.Stop();
                PerformanceTracker.Record("News", sw.Elapsed.TotalMilliseconds);
                Console.WriteLine($"Error calling API: {ex.Message}");
                return [];
            }
        }

        private async Task<List<AggregatedItemModel>> GetFromWeatherAsync()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var json = await _http.GetStringAsync("http://api.openweathermap.org/data/2.5/forecast?id=524901&appid=78e9be6df696a4648fb15499d2f8a1d8");

                sw.Stop();
                PerformanceTracker.Record("Weather", sw.Elapsed.TotalMilliseconds);

                var rawItems = System.Text.Json.JsonSerializer.Deserialize<List<Models.WeatherResponse>>(json);

                if (rawItems == null) return [];
                return rawItems.Select(x => new AggregatedItemModel
                {
                    Date = x.Date,
                   // Category = x.Category,
                    Source = "Weather"
                }).ToList();
            }
            catch (Exception ex)
            {
                sw.Stop();
                PerformanceTracker.Record("Weather", sw.Elapsed.TotalMilliseconds);
                Console.WriteLine($"Error calling API: {ex.Message}");
                return [];
            }
        }

        public async Task<List<AggregatedItemModel>> FetchAllDataAsync(string? category,
        string? sortBy = "date",
        string sortOrder = "asc")
        {

            // Try to get the whole aggregated data from cache
            if (_cache.TryGetValue("aggregated_data", out List<AggregatedItemModel>? cachedData))
            {
                return cachedData;
            }

            // Call all APIs in parallel
            var newsClient =
                GetCachedOrGetFromNewsAsync("news_data", GetFromNewsAsync(),
                TimeSpan.FromMinutes(1));
            var weatherClient =
                GetCachedOrGetFromWeatherAsync("weather_data", GetFromWeatherAsync(),
                TimeSpan.FromMinutes(1));

            await Task.WhenAll(weatherClient, newsClient);

            // Merge results
            var aggregatedData = new List<AggregatedItemModel>();
            aggregatedData.AddRange(weatherClient.Result);
            aggregatedData.AddRange(newsClient.Result);

            // Cache the aggregated result separately
            _cache.Set("aggregated_data", aggregatedData, TimeSpan.FromSeconds(30));

            // Filtering
            if (!string.IsNullOrEmpty(category))
            {
                aggregatedData = aggregatedData
                    .Where(x =>
                    x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sorting
            aggregatedData = sortBy?.ToLower() switch
            {
                "date" => sortOrder.ToLower() == "desc" ?
                aggregatedData
                .OrderByDescending(x => x.Date).ToList() :
                aggregatedData.OrderBy(x => x.Date).ToList(),
                "category" => sortOrder.ToLower() == "desc" ?
                aggregatedData.OrderByDescending(x => x.Category).ToList() :
                aggregatedData.OrderBy(x => x.Category).ToList(),
                _ => aggregatedData
            };

            return aggregatedData;
        }
        async Task<T?> SecureCall<T>(Task<T?> task)
        {
            try { return await task; }
            catch { return default; }
        }
    }
}
