using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Common;

namespace PurchaseApproval.Api.Middleware;

// Catches every error thrown anywhere in the API and returns clean JSON
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning("Request failed ({Status}): {Message}", ex.StatusCode, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number is 2601 or 2627))
        {
            _logger.LogWarning(ex, "Duplicate value rejected by the database");
            await WriteAsync(context, 409, "A record with the same value already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == 547)
        {
            _logger.LogWarning(ex, "A database rule rejected the change");
            await WriteAsync(context, 409, "This change conflicts with related data or a database rule.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, 500, "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string message,
        Dictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ApiError
        {
            Status = status,
            Message = message,
            Errors = errors,
            TraceId = context.TraceIdentifier
        });
    }
}
