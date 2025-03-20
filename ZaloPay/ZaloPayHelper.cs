using BookMoth_Api_With_C_.ZaloPay.Crypto;
using BookMoth_Api_With_C_.ZaloPay.Models;
using BookMoth_Api_With_C_.ZaloPay.Extension;

namespace BookMoth_Api_With_C_.ZaloPay
{
    public class ZaloPayHelper
    {
        private static long uid = Util.GetTimeStamp();
        private readonly IConfiguration _configuration;

        public ZaloPayHelper(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public bool VerifyCallback(string data, string requestMac)
        {
            try
            {
                string mac = HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, _configuration["ZaloPay:Key2"], data);

                return requestMac.Equals(mac);
            } catch
            {
                return false;
            }
        }

        public bool VerifyRedirect(Dictionary<string, object> data)
        {
            try
            {
                string reqChecksum = data["checksum"].ToString();
                string checksum = ZaloPayMacGenerator.Redirect(data, _configuration["ZaloPay:Key1"]);

                return reqChecksum.Equals(checksum);
            } catch
            {
                return false;
            }
        }

        public string GenTransID(int transCount)
        {
            return DateTime.Now.ToString("yyMMdd") + "_" + _configuration["ZaloPay:Appid"] + "_" + (++transCount); 
        }

        public Task<Dictionary<string, object>> CreateOrder(Dictionary<string, string> orderData)
        {
            return HttpHelper.PostFormAsync(_configuration["ZaloPay:ZaloPayApiCreateOrder"], orderData);
        }

        public Task<Dictionary<string, object>> CreateOrder(OrderData orderData)
        {
            return CreateOrder(orderData.AsParams());
        }

        public Task<Dictionary<string, object>> GetOrderStatus(string apptransid)
        {
            var data = new Dictionary<string, string>();
            data.Add("appid", _configuration["ZaloPay:Appid"]);
            data.Add("apptransid", apptransid);
            data.Add("mac", ZaloPayMacGenerator.GetOrderStatus(data, _configuration["ZaloPay:Key1"]));

            return HttpHelper.PostFormAsync(_configuration["ZaloPay:ZaloPayApiGetOrderStatus"], data);
        }

        public Task<Dictionary<string, object>> Refund(Dictionary<string, string> refundData)
        {
            return HttpHelper.PostFormAsync(_configuration["ZaloPay:ZaloPayApiRefund"], refundData);
        }

        public Task<Dictionary<string, object>> Refund(RefundData refundData)
        {
            return Refund(refundData.AsParams());
        }

        public Task<Dictionary<string, object>> GetRefundStatus(string mrefundid)
        {
            var data = new Dictionary<string, string>();
            data.Add("appid", _configuration["ZaloPay:Appid"]);
            data.Add("mrefundid", mrefundid);
            data.Add("timestamp", Util.GetTimeStamp().ToString());
            data.Add("mac", ZaloPayMacGenerator.GetRefundStatus(data, _configuration["ZaloPay:Key1"]));

            return HttpHelper.PostFormAsync(_configuration["ZaloPay:ZaloPayApiGetRefundStatus"], data);
        }
    }
}