using System;

namespace TIBG.Models
{
    public class ErrorResponse
    {
        public ErrorDetail Error { get; set; } = new ErrorDetail();

        public ErrorResponse() { }

        public ErrorResponse(string code, string message, int? retryAfter = null, string? supportId = null)
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                RetryAfter = retryAfter,
                SupportId = supportId ?? Guid.NewGuid().ToString()
            };
        }
    }

    public class ErrorDetail
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? RetryAfter { get; set; }
        public string? SupportId { get; set; }

        public object? Details { get; set; }
    }

    public static class ErrorCodes
    {
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string EMAIL_ALREADY_EXISTS = "EMAIL_ALREADY_EXISTS";
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
        public const string TOKEN_INVALID = "TOKEN_INVALID";
        public const string REFRESH_TOKEN_INVALID = "REFRESH_TOKEN_INVALID";
        public const string REFRESH_TOKEN_EXPIRED = "REFRESH_TOKEN_EXPIRED";
        public const string UNAUTHORIZED = "UNAUTHORIZED";

        public const string VALIDATION_FAILED = "VALIDATION_FAILED";
        public const string INVALID_INPUT = "INVALID_INPUT";
        public const string REQUIRED_FIELD_MISSING = "REQUIRED_FIELD_MISSING";

        public const string AI_SERVICE_UNAVAILABLE = "AI_SERVICE_UNAVAILABLE";
        public const string AI_SERVICE_TIMEOUT = "AI_SERVICE_TIMEOUT";
        public const string DATABASE_UNAVAILABLE = "DATABASE_UNAVAILABLE";
        public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";

        public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
        public const string TOO_MANY_REQUESTS = "TOO_MANY_REQUESTS";

        public const string INTERNAL_SERVER_ERROR = "INTERNAL_SERVER_ERROR";
        public const string UNEXPECTED_ERROR = "UNEXPECTED_ERROR";

        public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
        public const string RESOURCE_CONFLICT = "RESOURCE_CONFLICT";
    }
}
