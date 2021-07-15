using LineApiUseCaseSmartRetail.Interfaces;
using LineApiUseCaseSmartRetail.Models;
using LineApiUseCaseSmartRetail.Options;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail.Services
{
    /// <summary>
    /// LINE Payに関するサービス
    /// </summary>
    public class LinePayService : ILinePayService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly LinePayOptions linePayOptions;

        public LinePayService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<LinePayOptions> linePayOptions)
        {
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this.linePayOptions = linePayOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(linePayOptions));
        }

        public async Task<LinePayRequestPayload> RequestAsync(string requestJson)
        {
            var path = "/v3/payments/request";
            var response = await PostAsync(path, requestJson);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsAsync<LinePayRequestPayload>();
        }

        public async Task<LinePayConfirmPayload> ConfirmAsync(string transactionId, string requestJson)
        {
            var path = $"/v3/payments/{transactionId}/confirm";
            var response = await PostAsync(path, requestJson);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsAsync<LinePayConfirmPayload>();
        }

        /// <summary>
        /// LINE Pay APIへのPOSTリクエストを行う
        /// </summary>
        /// <param name="path">APIのパス</param>
        /// <param name="requestJson">リクエストBodyに使用するJSON</param>
        /// <returns>APIのレスポンス</returns>
        private async Task<HttpResponseMessage> PostAsync(string path, string requestJson)
        {
            if (string.IsNullOrWhiteSpace(requestJson))
            {
                return null;
            }

            // signature の作成
            // Base64(HMAC-SHA256(Your ChannelSecret, (Your ChannelSecret + URL Path + RequestBody + nonce)))
            var nonce = Guid.NewGuid().ToString().Replace("-", "");
            var key = Encoding.UTF8.GetBytes(linePayOptions.ChannelSecret);
            var bytes = Encoding.UTF8.GetBytes($"{linePayOptions.ChannelSecret}{path}{requestJson}{nonce}");
            byte[] hash;
            using (var hmac = new HMACSHA256(key))
            {
                hash = hmac.ComputeHash(bytes);
            }
            var signature = Convert.ToBase64String(hash);

            var client = httpClientFactory.CreateClient("linePay");
            client.DefaultRequestHeaders.Add("X-LINE-Authorization", signature);
            client.DefaultRequestHeaders.Add("X-LINE-Authorization-Nonce", nonce);

            return await client.PostAsync(path, new StringContent(requestJson, Encoding.UTF8, "application/json"));
        }
    }
}
