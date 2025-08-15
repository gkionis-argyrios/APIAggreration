using APIAggreration.Models;

namespace APIAggreration.Interfaces
{
    public interface IDataProviderService
    {
        Task<List<AggregatedItemModel>> FetchAllDataAsync(string? category,
        string? sortBy = "date",
        string? sortOrder = "asc");
    }
}
