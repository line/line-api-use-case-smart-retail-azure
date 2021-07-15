using LineApiUseCaseSmartRetail.Models;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail.Interfaces
{
    /// <summary>
    /// LINE Payに関するサービスのインターフェース
    /// </summary>
    public interface ILinePayService
    {
        /// <summary>
        /// Request APIを実行
        /// </summary>
        /// <param name="requestJson">リクエストBodyに使用するJSON</param>
        /// <returns>Request APIのレスポンス</returns>
        Task<LinePayRequestPayload> RequestAsync(string requestJson);

        /// <summary>
        /// Confirm APIを実行
        /// </summary>
        /// <param name="transactionId">Request APIで発行されたtransactionId</param>
        /// <param name="requestJson">リクエストBodyに使用するJSON</param>
        /// <returns>Confirm APIのレスポンス</returns>
        Task<LinePayConfirmPayload> ConfirmAsync(string transactionId, string requestJson);
    }
}
