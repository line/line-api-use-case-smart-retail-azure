using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail
{
    /// <summary>
    /// 未削除のクーポン一覧を取得するFunction
    /// </summary>
    public class GetCouponsInfo : BaseFunction
    {
        public GetCouponsInfo(CosmosClient client) : base(client)
        {
        }

        [FunctionName("get_coupons_info")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "get_coupons_info")] HttpRequest req,
            ILogger log)
        {
            var coupons = await GetNotDeletedCouponsAsync();

            return new OkObjectResult(coupons);
        }
    }
}
