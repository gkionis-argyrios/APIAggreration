using APIAggreration.Classes;
using APIAggreration.Interfaces;
using APIAggreration.Models;
using APIAggreration.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace APIAggreration.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize] 
    public class ProviderController : Controller
    {
        private readonly IDataProviderService _dataProviderService;

        public ProviderController(IDataProviderService dataProvider)
        {
            _dataProviderService = dataProvider;
        }

        [HttpGet("aggregated-data")]
        public async Task<IActionResult> GetAggregatedData(string? category,
        string? sortBy = "date",
        string? sortOrder = "asc")
        {
            var result = await _dataProviderService.FetchAllDataAsync(category, sortBy, sortOrder);
            return Ok(result);
        }

        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            var stats = PerformanceTracker.GetStats()
                .Select(kvp => new
                {
                    Api = kvp.Key,
                    kvp.Value.TotalRequests,
                    AverageResponseTimeMs = Math.Round(kvp.Value.AverageResponseTimeMs, 2),
                    Buckets = new
                    {
                        Fast = kvp.Value.FastCount,
                        Average = kvp.Value.AverageCount,
                        Slow = kvp.Value.SlowCount
                    }
                });

            return Ok(stats);
        }
    }
}
