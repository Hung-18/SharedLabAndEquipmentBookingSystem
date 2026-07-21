namespace API.Models;

public sealed record ApiErrorResponse(
    int StatusCode,
    string Message,
    DateTime Timestamp);
