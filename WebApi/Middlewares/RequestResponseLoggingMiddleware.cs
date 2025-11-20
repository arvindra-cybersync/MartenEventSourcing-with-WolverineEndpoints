using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApi.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Enable buffering to read the request body multiple times
            context.Request.EnableBuffering();

            string requestBody = await ReadRequestBody(context);

            _logger.LogInformation("HTTP REQUEST:\n" +
                                   "Path: {Path}\nMethod: {Method}\nBody: {Body}",
                context.Request.Path,
                context.Request.Method,
                requestBody);

            // Capture the response body
            var originalBodyStream = context.Response.Body;
            using var newBodyStream = new MemoryStream();
            context.Response.Body = newBodyStream;

            await _next(context);

            string responseBody = await ReadResponseBody(context.Response);
            _logger.LogInformation("HTTP RESPONSE:\n" +
                                   "Status Code: {StatusCode}\nBody: {Body}",
                context.Response.StatusCode,
                responseBody);

            // Reset the stream so ASP.NET can return it to the client
            newBodyStream.Seek(0, SeekOrigin.Begin);
            await newBodyStream.CopyToAsync(originalBodyStream);
        }

        private async Task<string> ReadRequestBody(HttpContext context)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }

        private async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return body;
        }
    }
}
