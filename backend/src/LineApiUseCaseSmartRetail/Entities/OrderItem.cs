using LineApiUseCaseSmartRetail.Enums;

namespace LineApiUseCaseSmartRetail.Entities
{
    /// <summary>
    /// 注文データが持つ商品情報
    /// </summary>
    public class OrderItem : Item
    {
        public int Quantity { get; set; }
        public string CouponId { get; set; }
        public DiscountWay DiscountWay { get; set; }
        public float DiscountRate { get; set; }

        public OrderItem() { }

        /// <summary>
        /// 商品データとクーポンデータをもとに初期化
        /// </summary>
        /// <param name="item"><see cref="Item"/></param>
        /// <param name="coupon"><see cref="Coupon"/></param>
        /// <param name="quantity">個数</param>
        public OrderItem(Item item, Coupon coupon, int quantity)
        {
            if (item != null)
            {
                Id = item.Id;
                Barcode = item.Barcode;
                ImageUrl = item.ImageUrl;
                ItemName = item.ItemName;
                ItemPrice = item.ItemPrice;
            }

            if (coupon != null)
            {
                CouponId = coupon.CouponId;
                DiscountWay = coupon.DiscountWay;
                DiscountRate = coupon.DiscountRate;
            }

            Quantity = quantity;
        }
    }
}
