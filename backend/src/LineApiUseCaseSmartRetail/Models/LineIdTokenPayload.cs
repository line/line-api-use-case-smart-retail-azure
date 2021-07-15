using System.Collections.Generic;

namespace LineApiUseCaseSmartRetail.Models
{
    /// <summary>
    /// IDトークン検証APIのレスポンスパース用
    /// </summary>
    public class LineIdTokenPayload
    {
        public string Iss { get; set; }
        public string Sub { get; set; }
        public string Aud { get; set; }
        public int Exp { get; set; }
        public int Iat { get; set; }
        public string Nonce { get; set; }
        public IEnumerable<string> Amr { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public string Email { get; set; }
    }
}
