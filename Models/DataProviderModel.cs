namespace APIAggreration.Models
{
    public static  class DataProviderModel
    {
        public record WeatherResponse(string dt_txt, string Category, string Source);
        public record NewsResponse(string Date, string Category, string Source);
    }
}
