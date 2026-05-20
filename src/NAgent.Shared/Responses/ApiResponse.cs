namespace NAgent.Shared.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "操作成功")
    {
        return new ApiResponse<T> 
        { 
            Success = true, 
            Message = message, 
            Data = data 
        };
    }

    public static ApiResponse<T> FailureResponse(string message)
    {
        return new ApiResponse<T> 
        { 
            Success = false, 
            Message = message 
        };
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse SuccessResponse(string message = "操作成功")
    {
        return new ApiResponse 
        { 
            Success = true, 
            Message = message 
        };
    }

    public static ApiResponse FailureResponse(string message)
    {
        return new ApiResponse 
        { 
            Success = false, 
            Message = message 
        };
    }
}
