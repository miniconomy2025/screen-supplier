namespace ScreenProducerAPI.Exceptions;

public abstract class BusinessException : Exception
{
    public string ErrorCode { get; }

    protected BusinessException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected BusinessException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}