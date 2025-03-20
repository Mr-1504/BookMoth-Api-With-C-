using Microsoft.Extensions.Caching.Memory;
using BookMoth_Api_With_C_.ZaloPay.Crypto;
using BookMoth_Api_With_C_.ZaloPay;
using Newtonsoft.Json;

namespace BookMoth_Api_With_C_.Services
{
    public class ZaloPayService
    {
        private static readonly string key1 = "9phuAOYhan4urywHTh0ndEXiV3pKHr5Q";
        private static readonly string queryOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/getstatusbyapptransid";
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;

        public ZaloPayService(IMemoryCache cache, IHttpClientFactory httpClientFactory)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetTransactionStatusAsync(string apptransid)
        {

            var param = new Dictionary<string, string>
            {
                { "appid", "553" },
                { "apptransid", apptransid }
            };

            string data = param["appid"] + "|" + param["apptransid"] + "|" + key1;
            param.Add("mac", HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, key1, data));

            var result = await HttpHelper.PostFormAsync(queryOrderUrl, param);
            return JsonConvert.SerializeObject(result);
        }
    }

}
