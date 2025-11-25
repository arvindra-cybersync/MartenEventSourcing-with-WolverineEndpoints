using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApi.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        // Sensitive fields that should be redacted from logs
        private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "secret", "apikey", "authorization",
            "api-key", "api_key", "auth", "bearer", "credential",
            "privatekey", "private_key", "sessionid", "session_id"
        };

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
            string sanitizedRequestBody = SanitizeBody(requestBody);

            // Check for sensitive headers
            var authHeader = context.Request.Headers.ContainsKey("Authorization") ? "[REDACTED]" : "None";

            _logger.LogInformation("HTTP REQUEST:\n" +
                                   "Path: {Path}\nMethod: {Method}\nAuthorization: {Auth}\nBody: {Body}",
                context.Request.Path,
                context.Request.Method,
                authHeader,
                sanitizedRequestBody);

            // Capture the response body
            var originalBodyStream = context.Response.Body;
            using var newBodyStream = new MemoryStream();
            context.Response.Body = newBodyStream;

            await _next(context);

            string responseBody = await ReadResponseBody(context.Response);
            string sanitizedResponseBody = SanitizeBody(responseBody);

            _logger.LogInformation("HTTP RESPONSE:\n" +
                                   "Status Code: {StatusCode}\nBody: {Body}",
                context.Response.StatusCode,
                sanitizedResponseBody);

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

        /// <summary>
        /// Sanitizes request/response body by redacting sensitive fields.
        /// Attempts to parse as JSON and redact known sensitive fields.
        /// </summary>
        private string SanitizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return body;

            try
            {
                // Try to parse as JSON
                using var document = JsonDocument.Parse(body);
                var sanitized = SanitizeJsonElement(document.RootElement);
                return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                // If not valid JSON, use regex to redact potential sensitive data patterns
                // Redact potential tokens/keys (long alphanumeric strings)
                body = Regex.Replace(body, @"\b[A-Za-z0-9]{32,}\b", "[REDACTED]");

                // Redact email-like patterns in some contexts
                foreach (var field in SensitiveFields)
                {
                    body = Regex.Replace(body,
                        $"\"{field}\"\\s*:\\s*\"[^\"]+\"",
                        $"\"{field}\": \"[REDACTED]\"",
                        RegexOptions.IgnoreCase);
                }

                return body;
            }
        }

        /// <summary>
        /// Recursively sanitizes JSON elements, redacting sensitive fields.
        /// </summary>
        private object SanitizeJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        if (SensitiveFields.Contains(property.Name))
                        {
                            obj[property.Name] = "[REDACTED]";
                        }
                        else
                        {
                            obj[property.Name] = SanitizeJsonElement(property.Value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    var arr = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        arr.Add(SanitizeJsonElement(item));
                    }
                    return arr;

                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null!;

                default:
                    return element.ToString() ?? string.Empty;
            }
        }
    }
}
