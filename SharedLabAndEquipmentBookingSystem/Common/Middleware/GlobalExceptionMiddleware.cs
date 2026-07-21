using Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace API.Common.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
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
                _logger.LogError(
                    ex,
                    "Unhandled exception: {Message}",
                    ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        public static Task HandleExceptionAsync(
            HttpContext context,
            Exception ex)
        {
            context.Response.ContentType = "application/json";

            if (ex is ResourceUnavailableException resourceUnavailableException)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;

                var conflictResponse = new
                {
                    statusCode = context.Response.StatusCode,
                    message = resourceUnavailableException.Message,
                    suggestedSlots = resourceUnavailableException.SuggestedSlots,
                    timestamp = DateTime.UtcNow
                };

                string conflictJson = JsonSerializer.Serialize(
                    conflictResponse,
                    JsonOptions);

                return context.Response.WriteAsync(conflictJson);
            }

            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string message = "Server Error";

            if (ex is DbUpdateException dbUpdateEx
                && dbUpdateEx.InnerException is SqlException sqlEx)
            {
                if (sqlEx.Number >= 50001 && sqlEx.Number <= 50006)
                {
                    statusCode = HttpStatusCode.Conflict;
                    message = sqlEx.Message;
                }
                else if (sqlEx.Number == 547)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Dữ liệu liên kết không tồn tại hoặc đang được sử dụng.";
                }
                else if (sqlEx.Number is 2601 or 2627)
                {
                    statusCode = HttpStatusCode.Conflict;
                    message = "Dữ liệu đã tồn tại.";
                }
                else
                {
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "Không thể lưu dữ liệu vào cơ sở dữ liệu.";
                }
            }
            else if (ex is KeyNotFoundException keyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = keyNotFoundException.Message;
            }
            else if (ex is UnauthorizedAccessException unauthorizedException)
            {
                statusCode = HttpStatusCode.Forbidden;
                message = unauthorizedException.Message;
            }
            else if (ex is InvalidOperationException invalidOperationException)
            {
                statusCode = HttpStatusCode.Conflict;
                message = invalidOperationException.Message;
            }
            else if (ex is ArgumentException argumentException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = argumentException.Message;
            }

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message,
                timestamp = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(response, JsonOptions);
            return context.Response.WriteAsync(json);
        }
    }
}
