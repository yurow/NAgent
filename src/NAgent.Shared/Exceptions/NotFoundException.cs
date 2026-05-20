namespace NAgent.Shared.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key) 
        : base($"实体 \"{name}\" ({key}) 不存在。")
    {
    }
}
