namespace BookMoth_Api_With_C_.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER_NAME = "x_key";
        private readonly string _apiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _apiKey = configuration["apiKey"];
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var key) || key != _apiKey)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API key is invalid or missing.");
                return;
            }

            await _next(context);
        }
    }
}
