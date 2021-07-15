using LineApiUseCaseSmartRetail.Entities;
using LineApiUseCaseSmartRetail.Interfaces;
using LineApiUseCaseSmartRetail.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail
{
    /// <summary>
    /// 注文情報を登録・更新するFunction
    /// </summary>
    public class PutCartData : BaseFunction
    {
        private readonly ILineService lineService;
        private readonly ApplicationOptions applicationOptions;
        private readonly LineOptions lineOptions;

        public PutCartData(
            CosmosClient client,
            ILineService lineService,
            IOptionsMonitor<ApplicationOptions> applicationOptions,
            IOptionsMonitor<LineOptions> lineOptions) : base(client)
        {
            this.lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
            this.applicationOptions = applicationOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(applicationOptions));
            this.lineOptions = lineOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(lineOptions));
        }

        [FunctionName("put_cart_data")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "put_cart_data")] PutCartDataRequest req,
            ILogger log)
        {
            // バリデーション
            if (!req.IsValid())
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

            // orderデータを取得（なければ新規作成）
            var order = !string.IsNullOrWhiteSpace(req.OrderId) ? await GetOrderAsync(req.OrderId) : new Order();
            order.UserId = userId;

            // order.item の作成
            var orderItems = new List<OrderItem>();
            foreach (var requestItem in req.Items)
            {
                var item = await GetItemByBarcodeAsync(requestItem.Barcode);

                // item単位のクーポンを取得
                var itemCoupon = await GetCouponByCouponIdAsync(requestItem.CouponId);

                if (item == null)
                {
                    // 商品情報がない場合は固定値を設定
                    var dummyItem = new Item
                    {
                        Barcode = requestItem.Barcode,
                        ImageUrl = "NO_DATA",
                        ItemName = "NO_DATA",
                        ItemPrice = 0,
                    };
                    orderItems.Add(new OrderItem(dummyItem, itemCoupon, requestItem.Quantity));

                    continue;
                }

                orderItems.Add(new OrderItem(item, itemCoupon, requestItem.Quantity));
            }
            order.Item = orderItems;

            // order単位のクーポンを取得
            var orderCoupon = await GetCouponByCouponIdAsync(req.CouponId);
            if (orderCoupon != null)
            {
                order.DiscountWay = orderCoupon.DiscountWay;
                order.DiscountRate = orderCoupon.DiscountRate;
            }

            // 金額算出
            order.CulcAmount();

            // delete_dayをセット（翌0時）
            var now = DateTime.UtcNow.ToJst();
            var today = now.AddHours(-now.Hour).AddMinutes(-now.Minute).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond).AddDays(1);
            order.ExpirationDate = (new DateTimeOffset(today)).ToUnixTimeSeconds();

            // order更新
            await UpsertOrderAsync(order);

            // 0円の場合はメッセージ送信
            if (order.Amount == 0)
            {
                log.LogInformation("金額が0円でした|orderId={orderId}", order.OrderId);
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
            }

            return new OkObjectResult(new { orderId = order.OrderId });
        }

        /// <summary>
        /// put_cart_data Functionのリクエスト
        /// </summary>
        public class PutCartDataRequest
        {
            public string IdToken { get; set; }
            public string OrderId { get; set; }
            public string CouponId { get; set; }
            public IEnumerable<PutCartDataItem> Items { get; set; }

            public class PutCartDataItem
            {
                public string Barcode { get; set; }
                public int Quantity { get; set; }
                public string CouponId { get; set; }
            }

            /// <summary>
            /// バリデーションを行う
            /// </summary>
            /// <returns>バリデーション結果(true:正常、false:異常)</returns>
            public bool IsValid()
            {
                // idToken必須
                if (string.IsNullOrWhiteSpace(IdToken))
                {
                    return false;
                }

                // items必須
                if (Items == null || !Items.Any())
                {
                    return false;
                }

                foreach (var item in Items)
                {
                    // バーコード必須
                    if (string.IsNullOrWhiteSpace(item.Barcode))
                    {
                        return false;
                    }

                    // 個数が0以下ならバリデーションエラー
                    if (item.Quantity < 1)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
