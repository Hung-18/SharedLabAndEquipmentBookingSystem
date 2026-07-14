using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class GlobalExceptionMiddleware
    {
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

            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Server Error";

            // Lỗi xảy ra khi EF Core lưu dữ liệu xuống SQL Server
            if (ex is DbUpdateException dbUpdateEx
                && dbUpdateEx.InnerException is SqlException sqlEx)
            {
                // Lỗi do trigger hoặc THROW trong SQL Server
                if (sqlEx.Number >= 50001
                    && sqlEx.Number <= 50006)
                {
                    statusCode = HttpStatusCode.Conflict;
                    message = sqlEx.Message;
                }
                // Lỗi khóa ngoại
                else if (sqlEx.Number == 547)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    message =
                        "Dữ liệu liên kết không tồn tại hoặc đang được sử dụng.";
                }
                // Lỗi trùng dữ liệu UNIQUE
                else if (sqlEx.Number == 2601
                    || sqlEx.Number == 2627)
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
            // Không tìm thấy dữ liệu
            else if (ex is KeyNotFoundException keyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = keyNotFoundException.Message;
            }
            // Không đủ quyền thực hiện chức năng
            else if (ex is UnauthorizedAccessException unauthorizedException)
            {
                statusCode = HttpStatusCode.Forbidden;
                message = unauthorizedException.Message;
            }
            // Lỗi nghiệp vụ: trùng lịch, trạng thái không hợp lệ...
            else if (ex is InvalidOperationException invalidOperationException)
            {
                statusCode = HttpStatusCode.Conflict;
                message = invalidOperationException.Message;
            }
            // Dữ liệu đầu vào không hợp lệ
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

            var json = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(json);
        }
    }
}