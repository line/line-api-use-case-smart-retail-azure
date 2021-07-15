using LineApiUseCaseSmartRetail.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail
{
    /// <summary>
    /// 商品情報を取得するFunction
    /// </summary>
    public class GetItemInfo : BaseFunction
    {
        public GetItemInfo(CosmosClient client) : base(client)
        {
        }

        [FunctionName("get_item_info")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "get_item_info")] GetItemInfoRequest req,
            ILogger log)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Barcode))
            {
                return new BadRequestObjectResult("パラメータ未設定エラー");
            }

            var item = await GetItemByBarcodeAsync(req.Barcode);
            if (item == null)
            {
                log.LogWarning("商品情報が取得できませんでした|barcode={barcode}", req.Barcode);
                return new BadRequestObjectResult("商品情報が取得できませんでした");
            }

            var response = new GetItemInfoResponse
            {
                Name = item.ItemName,
                Price = item.ItemPrice,
                ImageUrl = item.ImageUrl,
            };

            // クーポンがあれば取得
            if (!string.IsNullOrEmpty(req.CouponId))
            {
                var coupon = await GetCouponByCouponIdAsync(req.CouponId);
                if (coupon != null)
                {
                    response.DiscountWay = coupon.DiscountWay;
                    response.DiscountRate = coupon.DiscountRate;
                }
            }

            return new OkObjectResult(response);
        }

        /// <summary>
        /// get_item_info Functionのリクエスト
        /// </summary>
        public class GetItemInfoRequest
        {
            public string Barcode { get; set; }
            public string CouponId { get; set; }
        }

        /// <summary>
        /// get_item_info Functionのレスポンス
        /// </summary>
        public class GetItemInfoResponse
        {
            public string Name { get; set; }
            public float Price { get; set; }
            public string ImageUrl { get; set; }
            public DiscountWay DiscountWay { get; set; }
            public float DiscountRate { get; set; }
        }
    }
}
