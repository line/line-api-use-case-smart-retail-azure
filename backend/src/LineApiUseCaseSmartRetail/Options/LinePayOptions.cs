namespace LineApiUseCaseSmartRetail.Options
{
    /// <summary>
    /// LINE Payに関する設定
    /// </summary>
    public class LinePayOptions
    {
        /// <summary>
        /// LINE PayのチャネルID
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// LINE Payのチャネルシークレット
        /// </summary>
        public string ChannelSecret { get; set; }

        /// <summary>
        /// LINE Payの決済画面で使用する画像のURL
        /// </summary>
        public string PaymentImageUrl { get; set; }

        /// <summary>
        /// confirm時コールバック先のURL
        /// </summary>
        public string ConfirmUrl { get; set; }

        /// <summary>
        /// キャンセル時コールバック先のURL
        /// </summary>
        public string CancelUrl { get; set; }
    }
}
