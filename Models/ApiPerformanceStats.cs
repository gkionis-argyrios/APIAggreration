namespace APIAggreration.Models
{
    public class ApiPerformanceStats
    {
        public int TotalRequests { get; set; }
        public double AverageResponseTimeMs => 
            TotalRequests == 0 ? 0 : TotalResponseTimeMs / TotalRequests;
        public double TotalResponseTimeMs { get; set; }

        public int FastCount { get; set; }     // < 100ms
        public int AverageCount { get; set; }  // 100–200ms
        public int SlowCount { get; set; }     // > 200ms
    }
}
