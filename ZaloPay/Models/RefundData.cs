using BookMoth_Api_With_C_.ZaloPay.Crypto;

namespace BookMoth_Api_With_C_.ZaloPay.Models
{
    public class RefundData
    {
        public string Appid { get; set; }
        public string Zptransid { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; }
        public long Timestamp { get; set; }
        public string Mrefundid { get; set; }
        public string Mac { get; set; }

        public RefundData(string appId, long amount, string zptransid, string description = "", string key1 = "")
        {
            Appid = appId;
            Zptransid = zptransid;
            Amount = amount;
            Description = description;
           // Mrefundid = ZaloPayHelper.GenTransID(1);
            Timestamp = Util.GetTimeStamp();
            Mac = ComputeMac(key1);
        }

        public string GetMacData()
        {
            return Appid + "|" + Zptransid + "|" + Amount + "|" + Description + "|" + Timestamp;
        }

        public string ComputeMac(string key1)
        {
            return HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, key1, GetMacData());
        }
    }
}