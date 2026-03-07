using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Tripzo.Models;

namespace Tripzo.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                $"An error occurred while processing your request: {exception.Message}");

            var errorResponse = new ErrorResponse
            {
                Message = exception.Message,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case BadHttpRequestException:
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Title = "Bad Request";
                    break;

                case KeyNotFoundException:
                    errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Title = "Not Found";
                    break;

                case UnauthorizedAccessException:
                    errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Title = "Unauthorized";
                    break;

                case InvalidOperationException:
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Title = "Invalid Operation";
                    break;

                case ApplicationException:
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Title = "Application Error";
                    break;

                default:
                    errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Title = "Internal Server Error";
                    errorResponse.Message = "An internal server error occurred. Please try again later.";
                    break;
            }

            httpContext.Response.StatusCode = errorResponse.StatusCode;

            await httpContext
                .Response
                .WriteAsJsonAsync(errorResponse, cancellationToken);

            return true;
        }
    }
}
