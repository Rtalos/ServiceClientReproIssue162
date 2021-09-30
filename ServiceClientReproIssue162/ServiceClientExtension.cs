using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using System;
using System.Diagnostics;

namespace ServiceClientReproIssue162
{
    public static class ServiceClientExtension
    {
        private const string CACHEKEY_AZURETOKEN = "_AzureToken";

        public static void AddDynamicsServiceClient(this IServiceCollection services)
        {
            services.AddScoped<ServiceClient>(sp =>
            {
                var client = SetupCrmServiceClient(sp);
                //After Clone CurrentAccessToken is missing
                var clone = client.Clone();
                return clone;
            });
        }

        private static ServiceClient SetupCrmServiceClient(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ServiceClient>>();
            TraceControlSettings.TraceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), TraceLevel.Verbose.ToString());
            TraceControlSettings.AddTraceListener(new LoggerTraceListener("Microsoft.PowerPlatform.Dataverse.Client", logger));

            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var client = new ServiceClient(new Uri("DYNAMICS URL"), (resourceUri) =>
            {
                return System.Threading.Tasks.Task.FromResult(GetToken(cache));
            }, logger: logger);

            if (client.IsReady)
            {
                return client;
            }
            else throw client.LastException;
        }

        private static string GetToken(IMemoryCache cache)
        {
            if (!cache.TryGetValue(CACHEKEY_AZURETOKEN, out string cacheEntry))
            {
                var azureToken = GetAzureToken();
                cacheEntry = azureToken.Token;
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(3000));

                cache.Set(CACHEKEY_AZURETOKEN, cacheEntry, cacheEntryOptions);
            }

            return cacheEntry;
        }

        private static AccessToken GetAzureToken()
        {
            var credentials = new EnvironmentCredential();

            var context = new TokenRequestContext(scopes: new[] { "DYNAMICS URL/.default" });

            var token = credentials.GetToken(context);

            return token;
        }

        private class LoggerTraceListener : TraceListener
        {
            private readonly ILogger _logger;

            public LoggerTraceListener(string name, ILogger logger) : base(name)
            {
                _logger = logger;
            }

            public override void Write(string message)
            {
                _logger.LogDebug(message);
            }

            public override void WriteLine(string message)
            {
                _logger.LogDebug(message);
            }
        }
    }
}
