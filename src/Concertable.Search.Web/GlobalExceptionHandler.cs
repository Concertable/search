using Concertable.Kernel;
using Concertable.Kernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Concertable.Search.Web;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;
    private readonly IHostEnvironment env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        this.logger = logger;
        this.env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            problemDetails.Status = httpContext.Response.StatusCode;
            problemDetails.Title = "Unauthorized";
            problemDetails.Detail = exception.Message;
        }
        else if (exception is DomainException domainEx)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            problemDetails.Status = httpContext.Response.StatusCode;
            problemDetails.Title = "Bad Request";
            problemDetails.Detail = domainEx.Message;
        }
        else if (exception is HttpException httpEx)
        {
            httpContext.Response.StatusCode = (int)httpEx.StatusCode;
            problemDetails.Status = httpContext.Response.StatusCode;
            problemDetails.Title = httpEx.Title;
            problemDetails.Detail = httpEx.Detail;

            if (exception is BadRequestException badRequestEx && badRequestEx.ValidationErrors is not null)
            {
                problemDetails.Detail = httpEx.Message;
                problemDetails.Extensions["errors"] = badRequestEx.ValidationErrors;
            }
            else
            {
                problemDetails.Detail = httpEx.Message;
            }
        }
        else
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            problemDetails.Status = httpContext.Response.StatusCode;
            problemDetails.Title = "Internal Server Error";
            problemDetails.Detail = !env.IsProduction()
                ? exception.Message
                : "An unexpected error occurred.";

            if (!env.IsProduction())
            {
                problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
                problemDetails.Extensions["stackTrace"] = exception.ToString();

                if (exception.InnerException != null)
                {
                    problemDetails.Extensions["innerException"] = exception.InnerException.ToString();
                }
            }
        }

        logger.UnhandledException(exception);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
