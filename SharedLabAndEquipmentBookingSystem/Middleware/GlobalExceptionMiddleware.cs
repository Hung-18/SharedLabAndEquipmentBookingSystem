using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception", e.Message);

                await HandleExceptionAsync(context, e);
            }
        }

        public static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Server Error";

            // bắt lỗi trigger ném qua dbupdateexception của efcore
            if (ex is DbUpdateException dbUpdateEx && dbUpdateEx.InnerException is SqlException sqlEx)
            {
                if (sqlEx.Number >= 50001 && sqlEx.Number <= 50006)
                {
                    statusCode = HttpStatusCode.Conflict;
                    message = sqlEx.Message;
                }
            }
            else if (ex is ArgumentException ax)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = ax.Message;
            }
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = message,
                timestamp = DateTime.UtcNow
            };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

    }
}
