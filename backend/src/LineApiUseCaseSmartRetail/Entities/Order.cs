using LineApiUseCaseSmartRetail.Enums;
using LineApiUseCaseSmartRetail.Models;
using LineApiUseCaseSmartRetail.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LineApiUseCaseSmartRetail.Entities
{
    /// <summary>
    /// 注文
    /// </summary>
    public class Order : BaseEntity
    {
        public string OrderId => Id;
        public string UserId { get; set; }
        public float Amount { get; set; } = 0;
        public DiscountWay DiscountWay { get; set; }
        public float DiscountRate { get; set; }
        public long TransactionId { get; set; }
        public long ExpirationDate { get; set; }
        public string OrderDateTime { get; set; }
        public string PaidDateTime { get; set; }
        public string UpdateDateTime { get; set; }
        public IEnumerable<OrderItem> Item { get; set; }

        public Order()
        {
            // orderIdと共通させるため、コンストラクタでID採番を行う
            Id = Guid.NewGuid().ToString();
            OrderDateTime = DateTime.UtcNow.ToJst().ToString("yyyy/MM/dd HH:mm:ss");
        }

        /// <summary>
        /// amountを算出しセットする
        /// </summary>
        public void CulcAmount()
        {
            // item単位の割引
            foreach (var orderItem in Item)
            {
                var price = orderItem.ItemPrice.Discount(orderItem.DiscountWay, orderItem.DiscountRate);
                Amount += price * orderItem.Quantity;
            }

            // order単位の割引
            Amount = (float)Math.Floor(Amount.Discount(DiscountWay, DiscountRate));

            // 0以下の場合は0円とする
            if (Amount <= 0) Amount = 0;
        }

        /// <summary>
        /// LINE Pay Request APIのリクエストに使用するJSONを作成
        /// </summary>
        /// <param name="linePayOptions"><see cref="LinePayOptions"/></param>
        /// <returns>JSON文字列</returns>
        public string CreateLinePayRequestJson(LinePayOptions linePayOptions)
            => JsonSerializer.Serialize(new
            {
                amount = Amount,
                currency = "JPY",
                orderId = OrderId,
                packages = new[]
                {
                    new
                    {
                        id = "1",
                        amount = Amount,
                        name = "Use Caseストア新宿店",
                        products = new[]
                        {
                            new
                            {
                                name = "購入商品",
                                imageUrl = linePayOptions.PaymentImageUrl,
                                quantity = "1",
                                price = Amount,
                            },
                        },
                    },
                },
                redirectUrls = new
                {
                    confirmUrl = linePayOptions.ConfirmUrl,
                    cancelUrl = linePayOptions.CancelUrl,
                },
                options = new
                {
                    payment = new
                    {
                        capture = "True",
                    },
                    display = new
                    {
                        locale = "ja",
                    },
                },
            });

        /// <summary>
        /// LINE Pay Confirm APIのリクエストに使用するJSONを作成
        /// </summary>
        /// <param name="currency">通貨</param>
        /// <returns>JSON文字列</returns>
        public string CreateLinePayConfirmJson(string currency = "JPY")
            => JsonSerializer.Serialize(new
            {
                amount = Amount,
                currency,
            });

        /// <summary>
        /// LINE Messaging APIで使用するFlexメッセージJSONを作成
        /// </summary>
        /// <param name="detailsUrl">詳細ページのURL</param>
        /// <returns>JSON文字列</returns>
        public string CreateFlexMessageJson(string detailsUrl)
            => JsonSerializer.Serialize(new
            {
                to = UserId,
                messages = new[] { new FlexMessagePayload(Amount, $"{detailsUrl}?orderId={OrderId}") },
            }, new JsonSerializerOptions { IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
