using static APIAggreration.Models.DataProviderModel;

namespace APIAggreration.Classes
{
    public static class FallbackData
    {
        public static readonly Dictionary<string, object> Cache = new()
        {
            { "Weather", new WeatherResponse(new DateTime().ToShortDateString(), "1", "Weather") },
            { "News", new NewsResponse(new DateTime().ToShortDateString(), "2", "News") }
        };
    }
}
