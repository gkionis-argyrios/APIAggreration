using System.Text.Json.Serialization;

namespace APIAggreration.Models
{
    public class WeatherResponse
    {
        [JsonPropertyName("dt_txt")]
        public string Date { get; set; }
    }
}
