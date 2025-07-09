using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models;
using System.Net;
using System.Text.Json;

namespace ScreenProducerAPI.Middleware;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            InsufficientStockException stockEx => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = stockEx.ErrorCode,
                Message = stockEx.Message
            }),

            OrderNotFoundException notFoundEx => (HttpStatusCode.NotFound, new ErrorResponse
            {
                ErrorCode = notFoundEx.ErrorCode,
                Message = notFoundEx.Message
            }),

            InvalidOrderStateException stateEx => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = stateEx.ErrorCode,
                Message = stateEx.Message
            }),

            InsufficientFundsException fundsEx => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = fundsEx.ErrorCode,
                Message = fundsEx.Message
            }),

            InvalidRequestException requestEx => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = requestEx.ErrorCode,
                Message = requestEx.Message
            }),

            SystemConfigurationException configEx => (HttpStatusCode.InternalServerError, new ErrorResponse
            {
                ErrorCode = configEx.ErrorCode,
                Message = "System configuration error"
            }),

            ExternalServiceException serviceEx => (HttpStatusCode.BadGateway, new ErrorResponse
            {
                ErrorCode = serviceEx.ErrorCode,
                Message = "External service temporarily unavailable",
                Details = serviceEx.ServiceName
            }),

            BusinessException businessEx => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = businessEx.ErrorCode,
                Message = businessEx.Message
            }),

            ArgumentException => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = "INVALID_ARGUMENT",
                Message = "Invalid request parameters"
            }),

            InvalidOperationException => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                ErrorCode = "INVALID_OPERATION",
                Message = "Operation not valid in current state"
            }),

            _ => (HttpStatusCode.InternalServerError, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An unexpected error occurred"
            })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else if (exception is BusinessException businessException)
        {
            _logger.LogWarning("Business exception: {ErrorCode} - {Message}", businessException.ErrorCode, businessException.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {ExceptionType}", exception.GetType().Name);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}