using System.Net;
using System.Text.Json;
using Movie_Auth_Service.DTOs;
using Movie_Auth_Service.Exceptions;

namespace Movie_Auth_Service.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            var statusCode = exception switch
            {
                ValidationException  => HttpStatusCode.BadRequest,          // 400
                UnauthorizedException => HttpStatusCode.Unauthorized,       // 401
                NotFoundException    => HttpStatusCode.NotFound,            // 404
                ConflictException    => HttpStatusCode.Conflict,            // 409
                _                    => HttpStatusCode.InternalServerError  // 500
            };

            var response = new ErrorResponseDto
            {
                Message = exception is not Exception { } e
                    ? "An unexpected error occurred"
                    : GetSafeMessage(exception, statusCode),
                StatusCode = (int)statusCode,
                Timestamp = DateTime.UtcNow
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private static string GetSafeMessage(Exception ex, HttpStatusCode code)
        {
            // For 500 errors dont expose internal details
            return code == HttpStatusCode.InternalServerError
                ? "An unexpected error occurred. Please try again later."
                : ex.Message;
        }
    }
}