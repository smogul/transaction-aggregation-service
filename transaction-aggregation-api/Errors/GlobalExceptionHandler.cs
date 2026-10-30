using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using transaction_aggregation_domain.Exceptions;

namespace transaction_aggregation_api.Errors
{
    // One place that catches every error and turns it into a tidy answer (a "ProblemDetails").
    // So the controllers never need try/catch.
    //
    // Example: asking for account "9999999999" throws NotFoundException, and the caller receives
    //   404 { "title": "Not found", "detail": "Account '9999999999' was not found." }
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
                _ => (StatusCodes.Status500InternalServerError, "Something went wrong")
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled error while processing {Path}", httpContext.Request.Path);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // We never show the inside details of a crash to the caller, only to our own logs.
                Detail = status == StatusCodes.Status500InternalServerError ? "Please try again later." : exception.Message
            };

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);

            return true;
        }
    }
}
