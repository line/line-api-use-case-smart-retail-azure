using System.Collections.Generic;

namespace LineApiUseCaseSmartRetail.Models
{
    /// <summary>
    /// LINE Pay Confirm APIのレスポンスパース用
    /// </summary>
    public class LinePayConfirmPayload
    {
        public string ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
        public ConfirmInfo Info { get; set; }

        public class ConfirmInfo
        {
            public long TransactionId { get; set; }
            public string OrderId { get; set; }
            public IEnumerable<Payinfo> PayInfo { get; set; }
            public IEnumerable<Package> Packages { get; set; }
        }

        public class Payinfo
        {
            public string Method { get; set; }
            public int Amount { get; set; }
        }

        public class Package
        {
            public string Id { get; set; }
            public int Amount { get; set; }
            public int UserFeeAmount { get; set; }
            public IEnumerable<Product> Products { get; set; }
        }

        public class Product
        {
            public string Name { get; set; }
            public string ImageUrl { get; set; }
            public int Quantity { get; set; }
            public int Price { get; set; }
        }
    }
}
