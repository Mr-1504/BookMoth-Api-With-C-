using System.Diagnostics;
using System.Text.Json;

namespace BookMoth_Api_With_C_.ZaloPay
{
    public class NgrokHelper
    {
        public static string PublicUrl { get; private set; }

        public static void StartNgrok(string ngrokPath)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = ngrokPath,
                Arguments = "http 7100",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(processInfo);
        }

        public static async Task<string> GetPublicUrl(string ngrokUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetStringAsync(ngrokUrl);
                    var jsonDoc = JsonDocument.Parse(response);
                    var tunnels = jsonDoc.RootElement.GetProperty("tunnels");
                    PublicUrl = tunnels[0].GetProperty("public_url").GetString();
                    return PublicUrl;
                }
                catch
                {
                    return "";
                }
            }
        }

        public static Dictionary<string, object> CreateEmbeddataWithPublicUrl(Dictionary<string, object> embeddata)
        {
            if (!string.IsNullOrEmpty(PublicUrl))
            {
                embeddata["callbackurl"] = PublicUrl + "/api/transaction/callback";
            }
            return embeddata;
        }

        public static Dictionary<string, object> CreateEmbeddataWithPublicUrl()
        {
            return CreateEmbeddataWithPublicUrl(new Dictionary<string, object>());
        }
    }
}