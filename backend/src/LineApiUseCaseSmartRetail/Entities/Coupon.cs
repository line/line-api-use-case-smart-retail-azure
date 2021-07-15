using LineApiUseCaseSmartRetail.Enums;

namespace LineApiUseCaseSmartRetail.Entities
{
    /// <summary>
    /// クーポン
    /// </summary>
    public class Coupon : BaseEntity
    {
        public string Barcode { get; set; }
        public string CouponDescription { get; set; }
        public string CouponId { get; set; }
        public string Deleted { get; set; }
        public string DiscountEndDatetime { get; set; }
        public float DiscountRate { get; set; }
        public string DiscountStartDatetime { get; set; }
        public DiscountWay DiscountWay { get; set; }
        public string ImageUrl { get; set; }
        public string ItemName { get; set; }
        public string Remarks { get; set; }
    }
}
