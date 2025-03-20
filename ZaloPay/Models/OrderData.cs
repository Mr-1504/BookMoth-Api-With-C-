using BookMoth_Api_With_C_.ZaloPay.Crypto;
using Newtonsoft.Json;

namespace BookMoth_Api_With_C_.ZaloPay.Models
{
    public class OrderData
    {
        static string _appid = "553";
        static string key11 = "9phuAOYhan4urywHTh0ndEXiV3pKHr5Q";
        static string createOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/createorder";

        public string Appid { get; set; }
        public string Apptransid { get; set; }
        public long Apptime { get; set; }
        public string Appuser { get; set; }
        public string Item { get; set; }
        public string Embeddata { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; }
        public string Bankcode { get; set; }
        public string Mac { get; set; }
        
        public OrderData(
            int appid,long amount,
            string transid = "",
            int transCount = 0,
            string description = "", 
            object embeddata = null,
            string appuser = "",
            string key1 = "",
            object item = null
        )
        {
            Appid = _appid;
            Apptransid = transid;
            Apptime = Util.GetTimeStamp();
            Appuser = appuser;
            Amount = amount;
            Bankcode = "zalopayapp";
            Description = description;
            Embeddata = "{}";
            Item = JsonConvert.SerializeObject(item);
            Mac = ComputeMac(key11);
        }

        public virtual string GetMacData()
        {
            return Appid + "|" + Apptransid + "|" + Appuser + "|" + Amount + "|" + Apptime + "|" + Embeddata + "|" + Item;
        }

        public string ComputeMac(string key1)
        {
            return HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, key11, GetMacData());
        }
    }
}