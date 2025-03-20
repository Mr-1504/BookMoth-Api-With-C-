using BookMoth_Api_With_C_.ZaloPay.Crypto;

namespace BookMoth_Api_With_C_.ZaloPay
{
    public class ZaloPayMacGenerator
    {
        public static string Compute(string data, string key1 = "")
        {
            return HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, key1, data);
        }

        private static string CreateOrderMacData(Dictionary<string, string> order)
        {
            return order["appid"] + "|" + order["apptransid"] + "|" + order["appuser"] + "|" + order["amount"]
              + "|" + order["apptime"] + "|" + order["embeddata"] + "|" + order["item"];
        }

        public static string CreateOrder(Dictionary<string, string> order, string key1)
        {
            return Compute(CreateOrderMacData(order), key1);
        }

        public static string Refund(Dictionary<string, string> data, string key1)
        {
            return Compute(
                data["appid"] + "|" + data["zptransid"] 
                + "|" + data["amount"] + "|" 
                + data["description"] + "|" 
                + data["timestamp"],
                key1
            );
        }

        public static string GetOrderStatus(Dictionary<string, string> data, string key1)
        {
            return Compute(data["appid"] + "|" + data["apptransid"] + "|" + key1, key1);
        }

        public static string GetRefundStatus(Dictionary<string, string> data, string key1)
        {
            return Compute(data["appid"] + "|" + data["mrefundid"] + "|" + data["timestamp"], key1);
        }
        public static string Redirect(Dictionary<string, object> data, string key1)
        {
            return Compute(data["appid"] + "|" + data["apptransid"] + "|" + data["pmcid"] + "|" + data["bankcode"]
                + "|" + data["amount"] + "|" + data["discountamount"] + "|" + data["status"], key1);
        }
    }
}