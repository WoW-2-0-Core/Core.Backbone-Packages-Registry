using System.Net;
using System.Security.Authentication;
using Backbone.General.Exceptions.Abstractions.Exceptions.General;
using Backbone.General.Validations.Abstractions.Extensions;
using Backbone.General.Validations.FluentValidation.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BackbonePackagesService.Api.Middleware;

public class ExceptionFormatter
{
    private static readonly Dictionary<HttpStatusCode, int> StatusCodePriority = new()
    {
        { HttpStatusCode.Conflict, 1 },
        { HttpStatusCode.NotFound, 2 },
        { HttpStatusCode.BadRequest, 3 }
    };

    public (HttpStatusCode statusCode, ProblemDetails problemDetails) Format(Exception exception, bool isDevEnv)
    {
        return exception switch
        {
            ValidationException or AppFluentValidationException => MapValidationErrorsToProblemDetails(exception, isDevEnv),
            AppException appEx when appEx.TryGetValidationException<AppFluentValidationException>(out var valEx) =>
                MapValidationErrorsToProblemDetails(valEx!, isDevEnv),
            AppException appEx when !appEx.TryGetValidationException<AppFluentValidationException>(out _) => MapToProblemDetails(appEx, isDevEnv),
            _ => MapToProblemDetails(exception, isDevEnv)
        };
    }

    private (HttpStatusCode, ProblemDetails) MapValidationErrorsToProblemDetails(Exception exception, bool isDevEnv)
    {
        var (errors, statusCode) = exception switch
        {
            AppFluentValidationException appValEx when appValEx.Errors?.Any() ?? false => (appValEx.Errors.ToList(), appValEx.StatusCode),
            ValidationException valEx when valEx.Errors?.Any() ?? false => (valEx.Errors.ToList(), HttpStatusCode.BadRequest),
            _ => throw new ArgumentException("Exception type is not validation exception")
        };

        // Identify errors with priority
        var overridingError = errors
            .Where(e => Enum.TryParse<HttpStatusCode>(e.ErrorCode, out _))
            .Select(e => (StatusCode: Enum.Parse<HttpStatusCode>(e.ErrorCode), e.ErrorMessage))
            .OrderBy(e => StatusCodePriority.GetValueOrDefault(e.Item1))
            .FirstOrDefault();

        if (overridingError.ErrorMessage is not null)
            return (overridingError.Item1, new ProblemDetails
            {
                Status = (int)overridingError.StatusCode,
                Title = overridingError.ErrorMessage,
                Detail = isDevEnv ? GetDetailedError(exception) : null
            });

        // Group errors and format
        var errorsFormatted = errors
            .Where(e => !string.IsNullOrEmpty(e.PropertyName))
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToArray()
            );

        // Create problem details
        var problemDetails = new ValidationProblemDetails(errorsFormatted)
        {
            Status = (int)statusCode,
            Title = !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : GetTitle(exception),
            Detail = isDevEnv ? GetDetailedError(exception) : null
        };

        return (statusCode, problemDetails);
    }

    private static string GetDetailedError(Exception exception)
    {
        return $"{exception.Message}\n{exception.StackTrace}\n{JsonConvert.SerializeObject(exception, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        })}";
    }

    private (HttpStatusCode, ProblemDetails) MapToProblemDetails(AppException exception, bool isDevEnv)
    {
        // TODO : Use TryGetException to check fluent exception too
        if (exception.TryGetValidationException<AppFluentValidationException>(out var appFluentEx))
            return MapValidationErrorsToProblemDetails(appFluentEx!, isDevEnv);

        var problemDetails = new ProblemDetails
        {
            Status = (int)exception.StatusCode,
            Title = !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : GetTitle(exception),
            Detail = isDevEnv ? GetDetailedError(exception) : null
        };

        return (exception.StatusCode, problemDetails);
    }

    private (HttpStatusCode, ProblemDetails) MapToProblemDetails(Exception exception, bool isDevEnv)
    {
        var statusCode = GetStatusCode(exception);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : GetTitle(exception),
            Detail = isDevEnv ? GetDetailedError(exception) : null
        };

        return (statusCode, problemDetails);
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            AuthenticationException => HttpStatusCode.Unauthorized,
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            AppFluentValidationException => "One or more validation errors occurred.",
            AppException => "An error occurred",
            ValidationException => "Validation Error",
            AuthenticationException => "Unauthorized",
            UnauthorizedAccessException => "Forbidden",
            _ => "Internal Server Error"
        };
    }
}