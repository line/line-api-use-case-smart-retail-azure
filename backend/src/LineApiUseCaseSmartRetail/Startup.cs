using LineApiUseCaseSmartRetail;
using LineApiUseCaseSmartRetail.Interfaces;
using LineApiUseCaseSmartRetail.Options;
using LineApiUseCaseSmartRetail.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace LineApiUseCaseSmartRetail
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // 環境変数の設定
            builder.Services.AddOptions<ApplicationOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(ApplicationOptions)).Bind(settings);
                });
            builder.Services.AddOptions<LineOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(LineOptions)).Bind(settings);
                });
            builder.Services.AddOptions<LinePayOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(LinePayOptions)).Bind(settings);
                });

            // インターフェースの設定
            builder.Services.AddScoped<ILineService, LineService>();
            builder.Services.AddScoped<ILinePayService, LinePayService>();

            // Cosmosの設定
            builder.Services.AddScoped(services =>
            {
                var configuration = services.GetService<IConfiguration>() ?? throw new ArgumentNullException("configuration");
                var account = configuration.GetValue<string>("CosmosDbAccount");
                var key = configuration.GetValue<string>("CosmosDbKey");
                var connectionString = $"AccountEndpoint={account};AccountKey={key}";
                return new CosmosClient(connectionString, new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        IgnoreNullValues = true,
                        Indented = false,
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                });
            });

            // HttpClientの設定
            builder.Services.AddHttpClient("itemInfo", c => c.BaseAddress = new Uri("https://app.rakuten.co.jp"));
            builder.Services.AddHttpClient("line", c => c.BaseAddress = new Uri("https://api.line.me"));
            builder.Services.AddHttpClient("linePay", (services, c) =>
            {
                var linePayOptions = services.GetRequiredService<IOptionsMonitor<LinePayOptions>>()?.CurrentValue;
                c.BaseAddress = new Uri("https://sandbox-api-pay.line.me");
                c.DefaultRequestHeaders.Add("X-LINE-ChannelId", linePayOptions?.ChannelId);
            });
        }
    }
}
