namespace ScreenProducerAPI.Exceptions;

public class InsufficientStockException : BusinessException
{
    public InsufficientStockException(string itemType, int requested, int available)
        : base("INSUFFICIENT_STOCK", $"Insufficient {itemType}. Requested: {requested}, Available: {available}")
    {
    }
}

public class OrderNotFoundException : BusinessException
{
    public OrderNotFoundException(int orderId)
        : base("ORDER_NOT_FOUND", $"Order {orderId} not found")
    {
    }
}

public class InvalidOrderStateException : BusinessException
{
    public InvalidOrderStateException(int orderId, string currentState, string requiredState)
        : base("INVALID_ORDER_STATE", $"Order {orderId} is in state '{currentState}', but requires '{requiredState}'")
    {
    }
}

public class InsufficientFundsException : BusinessException
{
    public InsufficientFundsException(int required, int available)
        : base("INSUFFICIENT_FUNDS", $"Insufficient funds. Required: {required}, Available: {available}")
    {
    }
}

public class ExternalServiceException : BusinessException
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message)
        : base("EXTERNAL_SERVICE_ERROR", $"{serviceName}: {message}")
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base("EXTERNAL_SERVICE_ERROR", $"{serviceName}: {message}", innerException)
    {
        ServiceName = serviceName;
    }
}

public class InvalidRequestException : BusinessException
{
    public InvalidRequestException(string message)
        : base("INVALID_REQUEST", message)
    {
    }
}

public class SystemConfigurationException : BusinessException
{
    public SystemConfigurationException(string message)
        : base("SYSTEM_CONFIGURATION_ERROR", message)
    {
    }
}
