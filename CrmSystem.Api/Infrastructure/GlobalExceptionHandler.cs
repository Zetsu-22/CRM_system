using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace CrmSystem.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment env,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = Map(exception);

        // 5xx — ошибка сервера, логируем как Error. 4xx — как Warning.
        if (status >= 500)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning(exception, "Handled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;

        ProblemDetails details;

        if (exception is FluentValidationException fv)
        {
            var errors = fv.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            details = new ValidationProblemDetails(errors)
            {
                Status = status,
                Title = title,
                Type = type,
                Instance = httpContext.Request.Path,
            };
        }
        else
        {
            details = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = type,
                Detail = exception.Message,
                Instance = httpContext.Request.Path,
            };
        }

        if (env.IsDevelopment())
            details.Extensions["exception"] = exception.GetType().FullName;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = details,
        });
    }

    private static (int status, string title, string type) Map(Exception ex) => ex switch
    {
        FluentValidationException          => (StatusCodes.Status400BadRequest,  "Validation failed",   "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
        DataAnnotationsValidationException => (StatusCodes.Status400BadRequest,  "Validation failed",   "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
        KeyNotFoundException               => (StatusCodes.Status404NotFound,    "Resource not found",  "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
        UnauthorizedAccessException        => (StatusCodes.Status401Unauthorized,"Unauthorized",        "https://tools.ietf.org/html/rfc7235#section-3.1"),
        DbUpdateConcurrencyException       => (StatusCodes.Status409Conflict,    "Concurrency conflict","https://tools.ietf.org/html/rfc7231#section-6.5.8"),
        InvalidOperationException          => (StatusCodes.Status409Conflict,    "Conflict",            "https://tools.ietf.org/html/rfc7231#section-6.5.8"),
        _                                  => (StatusCodes.Status500InternalServerError, "Internal server error", "https://tools.ietf.org/html/rfc7231#section-6.6.1"),
    };
}
