using System.Net;
using Microsoft.Extensions.Logging.Console;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BookMoth_Api_With_C_.Middleware
{
    public class DdosDetectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DdosDetectionMiddleware> _logger;
        private static readonly string ModelPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "ddos_detection_ir9.onnx");
        private static InferenceSession session;
        private static readonly object sessionLock = new();
        private static readonly Dictionary<string, List<DateTime>> RequestTimes = new();

        static DdosDetectionMiddleware()
        {
            try
            {
                session = new InferenceSession(ModelPath);
                Console.WriteLine("[INFO] ONNX model loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load ONNX model: {ex.Message}");
            }
        }

        public DdosDetectionMiddleware(RequestDelegate next, ILogger<DdosDetectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Incoming request detected.");
            var packet = ExtractFeaturesFromRequest(context);
            NormalizeFeatures(packet);

            bool isDdos;
            lock (sessionLock)
            {
                isDdos = PredictDdos(packet);
            }

            _logger.LogInformation($"[INFO] DDoS prediction: {(isDdos ? " Attack detected" : " Normal traffic")}");
            Console.WriteLine();

            if (isDdos)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync(" DDoS Attack Detected! Request Blocked!");
                return;
            }
            await _next(context);
        }

        private NetworkPacket ExtractFeaturesFromRequest(HttpContext context)
        {
            var headers = context.Request.Headers;
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var now = DateTime.UtcNow;

            if (!RequestTimes.TryGetValue(ip, out var timestamps))
            {
                timestamps = new List<DateTime>();
                RequestTimes[ip] = timestamps;
            }
            timestamps.Add(now);
            RequestTimes[ip].RemoveAll(t => (now - t).TotalSeconds > 1);

            int totalFwdPackets = RequestTimes[ip].Count;
            int totalBwdPackets = totalFwdPackets / 2;
            float flowBytesPerSec = context.Request.ContentLength.HasValue ? context.Request.ContentLength.Value / 1.0f : 0;
            float flowPacketsPerSec = totalFwdPackets / 1.0f;

            return new NetworkPacket
            {
                Protocol = context.Request.Protocol switch
                {
                    "HTTP/1.1" => 6,
                    "HTTP/2" => 17,
                    _ => 0
                },
                TotalFwdPackets = totalFwdPackets,
                TotalBwdPackets = totalBwdPackets,
                FlowBytesPerSec = flowBytesPerSec,
                FlowPacketsPerSec = flowPacketsPerSec,
                SYN_Flag = headers.ContainsKey("Syn") ? 1 : 0,
                ACK_Flag = headers.ContainsKey("Ack") ? 1 : 0,
                RST_Flag = headers.ContainsKey("Rst") ? 1 : 0
            };
        }

        private void NormalizeFeatures(NetworkPacket packet)
        {
            packet.Protocol = packet.Protocol / 10.0f;
            packet.TotalFwdPackets /= 1000.0f;
            packet.TotalBwdPackets /= 1000.0f;
            packet.FlowBytesPerSec /= 10000.0f;
            packet.FlowPacketsPerSec /= 100.0f;
        }

        private bool PredictDdos(NetworkPacket packet)
        {
            try
            {
                var inputTensor = new DenseTensor<float>(
                    new float[] { packet.Protocol, packet.TotalFwdPackets, packet.TotalBwdPackets, packet.FlowBytesPerSec, packet.FlowPacketsPerSec, packet.SYN_Flag, packet.ACK_Flag, packet.RST_Flag },
                    new int[] { 1, 8 });

                var inputs = new NamedOnnxValue[] { NamedOnnxValue.CreateFromTensor("float_input", inputTensor) };
                using var results = session.Run(inputs);

                // Lấy nhãn dự đoán
                var labelTensor = results.First(r => r.Name == "output_label").AsTensor<long>();
                long predictedLabel = labelTensor.First();

                return predictedLabel != 1;  // Nếu là 1 thì DDoS, nếu 0 thì bình thường
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Model inference failed: {ex.Message}");
                return false;
            }
        }

    }

    public class NetworkPacket
    {
        public float Protocol { get; set; }
        public float TotalFwdPackets { get; set; }
        public float TotalBwdPackets { get; set; }
        public float FlowBytesPerSec { get; set; }
        public float FlowPacketsPerSec { get; set; }
        public float SYN_Flag { get; set; }
        public float ACK_Flag { get; set; }
        public float RST_Flag { get; set; }
    }
}
