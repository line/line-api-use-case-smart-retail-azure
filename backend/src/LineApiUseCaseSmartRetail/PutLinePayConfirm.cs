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
    /// LINE Pay Confirm APIを実行するFunction
    /// </summary>
    public class PutLinePayConfirm : BaseFunction
    {
        private readonly ILineService lineService;
        private readonly ILinePayService linePayService;
        private readonly ApplicationOptions applicationOptions;
        private readonly LineOptions lineOptions;

        public PutLinePayConfirm(
            CosmosClient client,
            ILineService lineService,
            ILinePayService linePayService,
            IOptionsMonitor<ApplicationOptions> applicationOptions,
            IOptionsMonitor<LineOptions> lineOptions) : base(client)
        {
            this.lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
            this.linePayService = linePayService ?? throw new ArgumentNullException(nameof(linePayService));
            this.applicationOptions = applicationOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(applicationOptions));
            this.lineOptions = lineOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(lineOptions));
        }

        [FunctionName("put_linepay_confirm")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "put_linepay_confirm")] PutLinePayConfirmRequest req,
            ILogger log)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.OrderId))
            {
                return new BadRequestObjectResult("パラメータ未設定エラー");
            }

            var order = await GetOrderAsync(req.OrderId);
            if (order == null)
            {
                log.LogWarning("Confirm APIに使用する注文データが見つかりませんでした|orderId={orderId}", req.OrderId);
                return new BadRequestObjectResult("注文データが見つかりませんでした");
            }

            // confirm APIをコール
            var response = await linePayService.ConfirmAsync(req.TransactionId.ToString(), order.CreateLinePayConfirmJson());
            if (response == null)
            {
                log.LogWarning("Confirm APIからエラーが返されました|orderId={orderId}|transactionId={transactionId}", req.OrderId, req.TransactionId);
                return new BadRequestObjectResult("決済に失敗しました");
            }

            // トランザクションIDを更新
            var now = DateTime.UtcNow.ToJst().ToString("yyyy/MM/dd HH:mm:ss");
            order.TransactionId = response.Info.TransactionId;
            order.ExpirationDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            order.PaidDateTime = now;
            order.UpdateDateTime = now;
            await UpsertOrderAsync(order);

            // メッセージ送信
            var lineChannel = await GetLineChannelAsync(lineOptions.ChannelId);
            if (lineChannel == null)
            {
                log.LogError("LINEチャネルアクセストークンがDBに登録されていません|channelId={channelId}", lineOptions.ChannelId);
                return new BadRequestObjectResult("決済完了メッセージの送信に失敗しました");
            }
            else if (lineChannel.IsExpired())
            {
                // 新しいチャネルアクセストークンを取得
                var channelAccessToken = await lineService.GetChannelAccessTokenAsync(lineChannel.ChannelId, lineChannel.ChannelSecret);
                if (string.IsNullOrWhiteSpace(channelAccessToken))
                {
                    log.LogError("新しいチャネルアクセストークンの取得に失敗しました|channelId={channelId}", lineChannel.ChannelId);
                    return new BadRequestObjectResult("決済完了メッセージの送信に失敗しました");
                }

                // チャネルアクセストークンを更新
                lineChannel.SetNewChannelAccessToken(channelAccessToken);
                var upsertResult = await UpsertLineChannelAsync(lineChannel);
                if (upsertResult == null)
                {
                    // ログのみ出力し、処理は続行する(トークンは取得できているため)
                    log.LogWarning("チャネルアクセストークンのデータ更新に失敗しました|channelId={channelId}", lineChannel.ChannelId);
                }
            }
            await lineService.PushMessageAsync(lineChannel.ChannelAccessToken, order.CreateFlexMessageJson(applicationOptions.DetailsUrl));

            return new OkObjectResult(response);
        }

        /// <summary>
        /// put_linepay_confirm Functionのリクエスト
        /// </summary>
        public class PutLinePayConfirmRequest
        {
            public string OrderId { get; set; }
            public long TransactionId { get; set; }
        }
    }
}
