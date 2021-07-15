using LineApiUseCaseSmartRetail.Interfaces;
using LineApiUseCaseSmartRetail.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail
{
    /// <summary>
    /// LINE Pay Request APIを実行するFunction
    /// </summary>
    public class PutLinePayRequest : BaseFunction
    {
        private readonly ILinePayService linePayService;
        private readonly LinePayOptions linePayOptions;

        public PutLinePayRequest(
            CosmosClient client,
            ILinePayService linePayService,
            IOptionsMonitor<LinePayOptions> linePayOptions) : base(client)
        {
            this.linePayService = linePayService ?? throw new ArgumentNullException(nameof(linePayService));
            this.linePayOptions = linePayOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(linePayOptions));
        }

        [FunctionName("put_linepay_request")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "put_linepay_request")] PutLinePayRequestRequest req,
            ILogger log)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.OrderId))
            {
                return new BadRequestObjectResult("パラメータ未設定エラー");
            }

            var order = await GetOrderAsync(req.OrderId);
            if (order == null)
            {
                log.LogWarning("Request APIに使用する注文データが見つかりませんでした|orderId={orderId}", req.OrderId);
                return new BadRequestObjectResult("注文データが見つかりませんでした");
            }

            // request APIをコール
            var response = await linePayService.RequestAsync(order.CreateLinePayRequestJson(linePayOptions));
            if (response == null)
            {
                log.LogWarning("Request APIからエラーが返されました|orderId={orderId}", req.OrderId);
                return new BadRequestObjectResult("決済に失敗しました");
            }

            return new OkObjectResult(response);
        }

        /// <summary>
        /// put_linepay_request Functionのリクエスト
        /// </summary>
        public class PutLinePayRequestRequest
        {
            public string OrderId { get; set; }
        }
    }
}
