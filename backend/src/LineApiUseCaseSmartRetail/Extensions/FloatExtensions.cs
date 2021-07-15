using LineApiUseCaseSmartRetail.Enums;

namespace System
{
    /// <summary>
    /// floatの拡張機能
    /// </summary>
    public static class FloatExtensions
    {
        /// <summary>
        /// 割引後の金額を取得
        /// </summary>
        /// <param name="f">割引元の金額</param>
        /// <param name="discountWay">割引種別</param>
        /// <param name="discountRate">割引率または割引金額</param>
        /// <returns></returns>
        public static float Discount(this float f, DiscountWay discountWay, float discountRate)
        {
            switch (discountWay)
            {
                case DiscountWay.PERCENTAGE:
                    return f * ((100 - discountRate) * 0.01f); // discountRateパーセントの割引
                case DiscountWay.PRICE:
                    return f - discountRate; // discountRate円の値引き
                case DiscountWay.NONE:
                default:
                    return f;
            }
        }
    }
}
