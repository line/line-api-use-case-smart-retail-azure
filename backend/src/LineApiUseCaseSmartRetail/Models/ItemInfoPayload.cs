using System;
using System.Collections.Generic;

namespace LineApiUseCaseSmartRetail.Models
{
    /// <summary>
    /// 商品情報APIのレスポンスパース用
    /// </summary>
    public class ItemInfoPayload
    {
        public IEnumerable<ProductItem> Products { get; set; }

        public class ProductItem
        {
            public Product Product { get; set; }
        }

        public class Product
        {
            public string SmallImageUrl { get; set; }
            public int UsedExcludeSalesMinPrice { get; set; }
            public string ProductName { get; set; }

            /// <summary>
            /// テスト用に調整した商品金額を取得
            /// </summary>
            /// <returns>調整後の金額</returns>
            public float GetPrice() => UsedExcludeSalesMinPrice > 0 ? UsedExcludeSalesMinPrice : 1; // 支払時に0円商品がエラーになるため1円に修正
        }
    }
}
