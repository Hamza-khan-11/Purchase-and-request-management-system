namespace PurchaseApproval.Api.Common;

// Throw one of these from a service when a business rule is broken.
// The error middleware turns it into a clean JSON response.
public class AppException : Exception
{
    public int StatusCode { get; }
    public Dictionary<string, string[]>? Errors { get; }

    public AppException(string message, int statusCode = 400, Dictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message, Dictionary<string, string[]>? errors = null)
        : base(message, 409, errors) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message, 403) { }
}
