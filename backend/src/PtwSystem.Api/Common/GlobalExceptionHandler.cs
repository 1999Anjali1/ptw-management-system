using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PtwSystem.Api.Common;

/// <summary>
/// Converts exceptions into RFC 7807 ProblemDetails JSON so the Angular app always gets a consistent error shape.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;

        if (exception is AppException app)
        {
            problem = new ProblemDetails
            {
                Status = app.StatusCode,
                Title = app.StatusCode switch
                {
                    StatusCodes.Status404NotFound => "Not found",
                    StatusCodes.Status403Forbidden => "Forbidden",
                    _ => "Request blocked by PTW rules"
                },
                Detail = app.Message
            };
            problem.Extensions["errors"] = app.Errors;
        }
        else
        {
            logger.LogError(exception, "Unhandled exception");
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected error",
                Detail = "Something went wrong. Please try again."
            };
        }

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
