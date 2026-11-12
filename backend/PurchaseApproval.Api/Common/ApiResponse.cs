namespace PurchaseApproval.Api.Common;

// Every successful response uses this shape
// helps wrapping data inside a consistent structure makes it easier 
//for your frontend service files to handle responses reliably

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Message = message, Data = data };
}

// Every error response uses this shape

public class ApiError
{
    public bool Success => false;
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public string? TraceId { get; set; }
}
