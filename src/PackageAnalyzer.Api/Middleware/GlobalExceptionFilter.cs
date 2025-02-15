using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PackageAnalyzer.Api.Middleware;

/// <summary>
/// Represents a global exception filter.
/// </summary>
public sealed class GlobalExceptionFilter(
    ILogger<GlobalExceptionFilter> logger,
    IHostEnvironment environment,
    ExceptionFormatter exceptionFormatter
) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.LogError(context.Exception, "An unhandled exception occurred");

        // Format exception
        var (statusCode, problemDetails) = exceptionFormatter.Format(
            context.Exception,
            environment.IsDevelopment()
        );
        problemDetails.Instance = context.HttpContext.Request.Path;

        // Set response
        context.Result = new ObjectResult(problemDetails) { StatusCode = (int)statusCode };
        context.ExceptionHandled = true;
    }
}