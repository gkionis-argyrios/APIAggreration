using APIAggreration.Classes;
using APIAggreration.Enums;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;
using static APIAggreration.Models.DataProviderModel;

namespace APIAggreration.Extensions
{
    public static class HttpClientRegistrationExtensions
    {
        public static IServiceCollection AddExternalApiClients(this IServiceCollection services, IConfiguration config)
        {
            RegisterApiClient<WeatherResponse>(ApiNames.Weather, FallbackData.Cache[ApiNames.Weather], services, config);
            RegisterApiClient<NewsResponse>(ApiNames.News, FallbackData.Cache[ApiNames.News], services, config);
            return services;
        }

        static void RegisterApiClient<T>(string name, object fallbackValue,
            IServiceCollection services, IConfiguration config)
        {
            // Central retry policy
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    3, //times to try
                    attempt => TimeSpan.FromMilliseconds(200 * attempt), //calculates the delay before each retry.
                    (outcome, timespan, retryAttempt, _) => //callback that runs each time a retry happens.
                    {
                        Console.WriteLine($"Retry {retryAttempt} " +
                            $"after {timespan.TotalMilliseconds}ms" +
                            $" due to {outcome.Exception?.Message}");
                    });

            // Central circuit breaker policy
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            //after 5 failures blocks calls Example: 30 seconds.

            //var fallbackPolicyNews = Policy
            //        .Handle<Exception>()
            //        .FallbackAsync(async (fallback) => await Resp(2)
            //        .ConfigureAwait(false));

            var json = JsonSerializer.Serialize(FallbackData.Cache[$"{name}"]);

            var fallbackPolicy = Policy<HttpResponseMessage>
                   .Handle<Exception>() // catch any exception
                   .FallbackAsync(
                     new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                     {
                         Content = 
                         new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                     });

            services.AddHttpClient(name, c =>
            {
                c.BaseAddress = new Uri(config[$"Jwt:Integrations:{name}:BaseUrl"]);
                c.DefaultRequestHeaders.Add(config["Jwt:Key"], config[$"Jwt:Integrations:{name}:ApiKey"]);
                c.DefaultRequestHeaders.Accept
                .Add(new System.Net.Http.Headers.
                MediaTypeWithQualityHeaderValue("application/json"));
            })
           .AddTransientHttpErrorPolicy(_ => retryPolicy)
           .AddTransientHttpErrorPolicy(_ => circuitBreakerPolicy)
           .AddPolicyHandler(fallbackPolicy);
        }

        //async static Task<HttpResponseMessage> Resp(short kind)
        //{
        //    var fallbackValue = FallbackData.Cache["Weather"];
        //    if (kind == 1)
        //    {
        //        fallbackValue = FallbackData.Cache["Weather"];
        //    }
        //    else if (kind == 2)
        //    {
        //        fallbackValue = FallbackData.Cache["News"];
        //    }
        //    var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        //    {
        //        Content = JsonContent.Create(fallbackValue)
        //    };
        //    return response;
        //}
    }
}
