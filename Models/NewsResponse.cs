using System.Text.Json.Serialization;

namespace APIAggreration.Models
{
    public class NewsResponse
    {
        [JsonPropertyName("publishedAt")]
        public string Date { get; set; }
    }
}
