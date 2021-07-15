using LineApiUseCaseSmartRetail.Models;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail.Interfaces
{
    /// <summary>
    /// LINEに関するサービスのインターフェース
    /// </summary>
    public interface ILineService
    {
        /// <summary>
        /// IDトークン検証APIの実行
        /// </summary>
        /// <param name="idToken">IDトークン</param>
        /// <returns>IDトークン検証APIのレスポンス（IDトークンのペイロード）</returns>
        Task<LineIdTokenPayload> VerifyIdTokenAsync(string idToken);

        /// <summary>
        /// Push APIの実行
        /// </summary>
        /// <param name="channelAccessToken">チャネルアクセストークン</param>
        /// <param name="requestJson">リクエストBodyに使用するJSON</param>
        /// <returns></returns>
        Task PushMessageAsync(string channelAccessToken, string requestJson);

        /// <summary>
        /// チャネルアクセストークン発行APIの実行
        /// </summary>
        /// <param name="channelId">チャネルID</param>
        /// <param name="channelSecret">チャネルシークレット</param>
        /// <returns>チャネルアクセストークン</returns>
        Task<string> GetChannelAccessTokenAsync(string channelId, string channelSecret);
    }
}
