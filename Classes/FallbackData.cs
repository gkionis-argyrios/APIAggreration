using APIAggreration.Enums;
using static APIAggreration.Models.DataProviderModel;

namespace APIAggreration.Classes
{
    public static class FallbackData
    {
        public static readonly Dictionary<string, object> Cache = new()
        {
            { ApiNames.Weather, new WeatherResponse(new DateTime().ToShortDateString(), "1", ApiNames.Weather) },
            {  ApiNames.News, new NewsResponse(new DateTime().ToShortDateString(), "2", ApiNames.News) }
        };
    }
}
