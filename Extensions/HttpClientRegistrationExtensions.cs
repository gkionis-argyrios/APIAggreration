using APIAggreration.Classes;
using Polly;
using Polly.Extensions.Http;
using static APIAggreration.Models.DataProviderModel;

namespace APIAggreration.Extensions
{
    public static class HttpClientRegistrationExtensions
    {
        public static IServiceCollection AddExternalApiClients(this IServiceCollection services)
        {
            // fallback cache
            RegisterApiClient<WeatherResponse>("Weather", FallbackData.Cache["Weather"], services);
            RegisterApiClient<NewsResponse>("News", FallbackData.Cache["News"], services);
            return services;
        }

        static void RegisterApiClient<T>(string name, object fallbackValue, IServiceCollection services)
        {
            // Central retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    (outcome, timespan, retryAttempt, _) =>
                    {
                        Console.WriteLine($"Retry {retryAttempt} " +
                            $"after {timespan.TotalMilliseconds}ms" +
                            $" due to {outcome.Exception?.Message}");
                    });

            // Central circuit breaker
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            // FallbackPolicy
            var fallbackPolicyWeather = Policy
                .Handle<Exception>()
                .FallbackAsync(async (fallback) => await Resp(1)
                .ConfigureAwait(false));

            var fallbackPolicyNews = Policy
                .Handle<Exception>()
                .FallbackAsync(async (fallback) => await Resp(2)
                .ConfigureAwait(false));

            services.AddHttpClient(name, c =>
            {
                c.BaseAddress = new Uri("https://newsapi.org/v2/top-headlines?sources=bbc-news&apiKey=");
                c.DefaultRequestHeaders.Add("ApiKey", "02be176b87ed479b885fce22a14eea79");
                c.DefaultRequestHeaders.Accept
                .Add(new System.Net.Http.Headers.
                MediaTypeWithQualityHeaderValue("application/json"));
            })
           .AddTransientHttpErrorPolicy(_ => retryPolicy)
           .AddTransientHttpErrorPolicy(_ => circuitBreakerPolicy);
           //.AddPolicyHandler((IAsyncPolicy<HttpResponseMessage>)fallbackPolicyNews);

            services.AddHttpClient(name, c =>
            {
                c.BaseAddress = new Uri("http://api.openweathermap.org/data/2.5/forecast?id=524901&appid=");
                c.DefaultRequestHeaders.Add("ApiKey", "78e9be6df696a4648fb15499d2f8a1d8");
                c.DefaultRequestHeaders.Accept
                .Add(new System.Net.Http.Headers.
                MediaTypeWithQualityHeaderValue("application/json"));
            })
           .AddTransientHttpErrorPolicy(_ => retryPolicy)
           .AddTransientHttpErrorPolicy(_ => circuitBreakerPolicy);
           //.AddPolicyHandler((IAsyncPolicy<HttpResponseMessage>)fallbackPolicyWeather);
        }

        async static Task<HttpResponseMessage> Resp(short kind)
        {
            var fallbackValue = FallbackData.Cache["Weather"];
            if (kind == 1)
            {
                fallbackValue = FallbackData.Cache["Weather"];
            }
            else if (kind == 2)
            {
                fallbackValue = FallbackData.Cache["News"];
            }
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(fallbackValue)
            };
            return response;
        }
    }
}
