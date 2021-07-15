using LineApiUseCaseSmartRetail.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail
{
    /// <summary>
    /// 注文情報を取得するFunction
    /// </summary>
    public class GetOrderInfo : BaseFunction
    {
        private readonly ILineService lineService;

        public GetOrderInfo(
            CosmosClient client,
            ILineService lineService) : base(client)
        {
            this.lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
        }

        [FunctionName("get_order_info")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "get_order_info")] GetOrderInfoRequest req,
            ILogger log)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.IdToken))
            {
                return new BadRequestObjectResult("パラメータ未設定エラー");
            }

            // ユーザーID取得
            var userProfile = await lineService.VerifyIdTokenAsync(req.IdToken);
            if (userProfile == null)
            {
                log.LogWarning("idToken is invalid.");
                return new ForbidResult();
            }
            var userId = userProfile.Sub;

            // 注文履歴を取得
            var orderId = req.OrderId;
            var orders = !string.IsNullOrWhiteSpace(orderId)
                ? await GetOrdersAsync(userId, orderId)
                : await GetOrdersAsync(userId);

            return new OkObjectResult(orders);
        }

        /// <summary>
        /// get_order_info のリクエスト
        /// </summary>
        public class GetOrderInfoRequest
        {
            public string IdToken { get; set; }
            public string OrderId { get; set; }
        }
    }
}
