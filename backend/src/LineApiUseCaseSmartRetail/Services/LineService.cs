using LineApiUseCaseSmartRetail.Interfaces;
using LineApiUseCaseSmartRetail.Models;
using LineApiUseCaseSmartRetail.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail.Services
{
    /// <summary>
    /// LINEに関するサービス
    /// </summary>
    public class LineService : ILineService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly LineOptions lineOptions;

        public LineService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<LineOptions> lineOptions)
        {
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this.lineOptions = lineOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(lineOptions));
        }

        public async Task<LineIdTokenPayload> VerifyIdTokenAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            // https://developers.line.biz/ja/reference/line-login/#verify-id-token
            var client = httpClientFactory.CreateClient("line");
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "id_token", idToken },
                { "client_id", lineOptions.LoginChannelId },
            });
            var response = await client.PostAsync("/oauth2/v2.1/verify", formData);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsAsync<LineIdTokenPayload>();
        }

        public async Task PushMessageAsync(string channelAccessToken, string requestJson)
        {
            var client = httpClientFactory.CreateClient("line");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken);
            await client.PostAsync("/v2/bot/message/push", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        }

        public async Task<string> GetChannelAccessTokenAsync(string channelId, string channelSecret)
        {
            if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(channelSecret))
            {
                return null;
            }

            var client = httpClientFactory.CreateClient("line");
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", channelId },
                { "client_secret", channelSecret },
            });
            var response = await client.PostAsync("/v2/oauth/accessToken", formData);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadAsAsync<LineChannelAccessTokenPayload>();
            return payload?.AccessToken;
        }
    }
}
