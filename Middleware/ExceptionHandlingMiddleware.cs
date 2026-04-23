using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Quay27.Application.Common.Exceptions;

namespace Quay27_Be.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is UpstreamDependencyException ude)
            {
                _logger.LogWarning(
                    ex,
                    "Upstream dependency exception. code={ErrorCode}",
                    ude.ErrorCode);
            }
            else
            {
                _logger.LogError(ex, "Unhandled exception");
            }
            await WriteProblemAsync(context, ex, _env);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception ex, IWebHostEnvironment env)
    {
        context.Response.ContentType = "application/problem+json";

        var problem = ex switch
        {
            NotFoundException nf => new ProblemDetails
            {
                Title = "Not Found",
                Detail = nf.Message,
                Status = (int)HttpStatusCode.NotFound
            },
            ForbiddenException fe => new ProblemDetails
            {
                Title = "Forbidden",
                Detail = fe.Message,
                Status = (int)HttpStatusCode.Forbidden
            },
            ConflictException ce => new ProblemDetails
            {
                Title = "Conflict",
                Detail = ce.Message,
                Status = (int)HttpStatusCode.Conflict
            },
            AppValidationException ave => new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = ave.Message,
                Status = (int)HttpStatusCode.BadRequest
            },
            ValidationException ve => new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", ve.Errors.Select(e => e.ErrorMessage)),
                Status = (int)HttpStatusCode.BadRequest,
                Extensions = { ["errors"] = ve.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
            },
            UpstreamDependencyException ude => new ProblemDetails
            {
                Title = "Upstream Dependency Failed",
                Detail = ude.Message,
                Status = (int)HttpStatusCode.BadGateway,
                Extensions = { ["errorCode"] = ude.ErrorCode }
            },
            _ => new ProblemDetails
            {
                Title = "Server Error",
                Detail = env.IsDevelopment()
                    ? ex.ToString()
                    : "An unexpected error occurred.",
                Status = (int)HttpStatusCode.InternalServerError
            }
        };

        context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
