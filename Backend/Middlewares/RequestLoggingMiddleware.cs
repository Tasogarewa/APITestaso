using System.Diagnostics;

namespace Backend.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Handling request {Method} {Path}", context.Request.Method, context.Request.Path);

                await _next(context); 

                stopwatch.Stop();
                _logger.LogInformation("Finished request {Method} {Path} with status {StatusCode} in {ElapsedMilliseconds} ms",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request {Method} {Path} failed after {ElapsedMilliseconds} ms",
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);

                throw; 
            }
        }
    }
}
