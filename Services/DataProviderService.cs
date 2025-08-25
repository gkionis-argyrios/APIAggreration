using APIAggreration.Classes;
using APIAggreration.Enums;
using APIAggreration.Interfaces;
using APIAggreration.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace APIAggreration.Services
{
    public class DataProviderService : IDataProviderService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        public DataProviderService(HttpClient http, IMemoryCache cache, IConfiguration config)
        {
            _http = http;
            _cache = cache;
            _config = config;
        }

        private async Task<List<AggregatedItemModel>> GetCachedOrGetFromNewsAsync(
            string cacheKey,
            Task<List<AggregatedItemModel>> fetchFunc,
            TimeSpan duration)
        {
            if (_cache.TryGetValue(cacheKey, out List<AggregatedItemModel>? cached))
            {
                return cached ?? [];
            }

            var data = await fetchFunc;
            if (data != null && data.Count > 0) _cache.Set(cacheKey, data, duration);
            return data ?? [];
        }

        private async Task<List<AggregatedItemModel>> GetCachedOrGetFromWeatherAsync(
            string cacheKey,
            Task<List<AggregatedItemModel>> fetchFunc,
            TimeSpan duration)
        {
            if (_cache.TryGetValue(cacheKey, out List<AggregatedItemModel>? cached))
            {
                return cached ?? [];
            }

            var data = await fetchFunc;
            if (data != null && data.Count > 0) _cache.Set(cacheKey, data, duration);
            return data ?? [];
        }

        public List<T>? DeserializeList<T>(string json)
        {
            return JsonSerializer.Deserialize<List<T>>(json);
        }

        private async Task<List<AggregatedItemModel>> GetFromAsync(string apiName, string url)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var json = await _http.GetStringAsync(url);
                sw.Stop();
                PerformanceTracker.Record(apiName, sw.Elapsed.TotalMilliseconds);

                if (apiName == ApiNames.News)
                {
                    List<NewsResponse>? rawItems = DeserializeList<NewsResponse>(json);
                    
                    if (rawItems == null) return [];
                    return rawItems.Select(x => new AggregatedItemModel
                    {
                        Date = x.Date,
                        // Category = x.Category,
                        Source = apiName
                    }).ToList();
                }
                else
                {
                    List<NewsResponse>? rawItems = DeserializeList<NewsResponse>(json);
                    sw.Stop();
                    PerformanceTracker.Record(apiName, sw.Elapsed.TotalMilliseconds);
                    if (rawItems == null) return [];
                    return rawItems.Select(x => new AggregatedItemModel
                    {
                        Date = x.Date,
                        // Category = x.Category,
                        Source = apiName
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                PerformanceTracker.Record(apiName, sw.Elapsed.TotalMilliseconds);
                Console.WriteLine($"Error calling API: {ex.Message}");
                return [];
            }
        }

        public async Task<List<AggregatedItemModel>> FetchAllDataAsync(string? category,
        string? sortBy = "date",
        string sortOrder = "asc")
        {
            var aggregatedData = new List<AggregatedItemModel>();
            List<AggregatedItemModel> cachedData = [];
            // Try to get the whole aggregated data from cache
            if (_cache.TryGetValue("aggregated_data", out List<AggregatedItemModel>? cached))
            {
                cachedData = cached ?? [];
            }

            if (cachedData == null || cachedData.Count == 0)
            {
                // Call all APIs in parallel
                //data expires at 1 min
                var urlApiWeather = $"{_config["Integrations:Weather:BaseUrl"]}{_config["Integrations:Weather:ApiKey"]}";
                var urlApiNews = $"{_config["Integrations:News:BaseUrl"]}{_config["Integrations:News:ApiKey"]}";
                var newsClient =
                    GetCachedOrGetFromNewsAsync("news_data", GetFromAsync(ApiNames.News, urlApiNews),
                    TimeSpan.FromMinutes(1));
                var weatherClient =
                    GetCachedOrGetFromWeatherAsync("weather_data", GetFromAsync(ApiNames.Weather, urlApiWeather),
                    TimeSpan.FromMinutes(1));

                await Task.WhenAll(weatherClient, newsClient);

                // Merge results
                aggregatedData.AddRange(weatherClient.Result);
                aggregatedData.AddRange(newsClient.Result);

                //Cache the aggregated result separately
                if (aggregatedData == null || aggregatedData.Count == 0) return [];

                _cache.Set("aggregated_data", aggregatedData, TimeSpan.FromSeconds(30));

                aggregatedData = filterData(category, aggregatedData);
                aggregatedData = sortData(aggregatedData, sortBy, sortOrder);
            }
            else
            {
                aggregatedData = filterData(category, cachedData);
                aggregatedData = sortData(cachedData, sortBy, sortOrder);
            }

            return aggregatedData;
        }

        private List<AggregatedItemModel> filterData(string? category,
            List<AggregatedItemModel> aggregatedData)
        {
            // Filtering
            if (!string.IsNullOrEmpty(category))
            {
                return aggregatedData
                    .Where(x =>
                    x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else return aggregatedData;
        }

        private List<AggregatedItemModel> sortData(List<AggregatedItemModel> aggregatedData,
            string? sortBy = "date",
        string sortOrder = "asc")
        {
            // Sorting
            return sortBy?.ToLower() switch
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
        }

        async Task<T?> SecureCall<T>(Task<T?> task)
        {
            try { return await task; }
            catch { return default; }
        }
    }
}
