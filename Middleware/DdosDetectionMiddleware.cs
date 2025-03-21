using System.Net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BookMoth_Api_With_C_.Middleware
{
    public class DdosDetectionMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string ModelPath = Path
            .Combine(Directory.GetCurrentDirectory(), "Resources", "ddos_detection_ir9.onnx");
        private static InferenceSession session;
        private static float[] featureMeans = { 500, 64, 6, 1000, 5000 };
        private static float[] featureStdDevs = { 200, 10, 2, 500, 2000 };

        static DdosDetectionMiddleware()
        {
            session = new InferenceSession(ModelPath);
        }

        public DdosDetectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var packet = ExtractFeaturesFromRequest(context);
            NormalizeFeatures(packet);
            var isDdos = PredictDdos(packet);

            if (isDdos)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("🚨 DDoS Attack Detected! Request Blocked! 🚨");
                return;
            }
            await _next(context);
        }

        private NetworkPacket ExtractFeaturesFromRequest(HttpContext context)
        {
            return new NetworkPacket
            {
                PacketLength = context.Request.ContentLength ?? 0,
                TTL = 64,
                Protocol = context.Request.Protocol == "HTTP/1.1" ? 6 : 17,
                IP_Length = context.Request.Headers.Count,
                PacketID = (uint)new Random().Next(1000, 9999)
            };
        }

        private void NormalizeFeatures(NetworkPacket packet)
        {
            packet.PacketLength = (packet.PacketLength - featureMeans[0]) / featureStdDevs[0];
            packet.TTL = (packet.TTL - featureMeans[1]) / featureStdDevs[1];
            packet.Protocol = (packet.Protocol - featureMeans[2]) / featureStdDevs[2];
            packet.IP_Length = (packet.IP_Length - featureMeans[3]) / featureStdDevs[3];
            packet.PacketID = (packet.PacketID - featureMeans[4]) / featureStdDevs[4];
        }

        private bool PredictDdos(NetworkPacket packet)
        {
            var inputTensor = new DenseTensor<float>(new float[] { packet.PacketLength, packet.TTL, packet.Protocol, packet.IP_Length, packet.PacketID }, new int[] { 1, 5 });
            var inputs = new NamedOnnxValue[] { NamedOnnxValue.CreateFromTensor("float_input", inputTensor) };
            using var results = session.Run(inputs);
            var prediction = results.First().AsEnumerable<float>().First();
            return prediction > 0.5f;
        }
    }

    public static class DdosDetectionMiddlewareExtensions
    {
        public static IApplicationBuilder UseDdosDetection(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DdosDetectionMiddleware>();
        }
    }

    public class NetworkPacket
    {
        public float PacketLength { get; set; }
        public float TTL { get; set; }
        public float Protocol { get; set; }
        public float IP_Length { get; set; }
        public float PacketID { get; set; }
    }
}
