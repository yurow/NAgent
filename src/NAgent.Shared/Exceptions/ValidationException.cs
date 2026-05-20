namespace NAgent.Shared.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(IEnumerable<string> errors) 
        : base($"验证失败: {string.Join(", ", errors)}")
    {
    }
}
