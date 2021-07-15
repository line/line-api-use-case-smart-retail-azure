namespace LineApiUseCaseSmartRetail.Models
{
    /// <summary>
    /// LINE Pay Request APIのレスポンスパース用
    /// </summary>
    public class LinePayRequestPayload
    {
        public string ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
        public RequestInfo Info { get; set; }

        public class RequestInfo
        {
            public Paymenturl PaymentUrl { get; set; }
            public long TransactionId { get; set; }
            public string PaymentAccessToken { get; set; }
        }

        public class Paymenturl
        {
            public string Web { get; set; }
            public string App { get; set; }
        }
    }
}
