namespace ScreenProducerAPI.Commands;

public class CommandResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public bool ShouldRetry { get; }

    private CommandResult(bool success, string? errorMessage = null, bool shouldRetry = false)
    {
        Success = success;
        ErrorMessage = errorMessage;
        ShouldRetry = shouldRetry;
    }

    public static CommandResult Succeeded() => new(true);

    public static CommandResult Failed(string errorMessage, bool shouldRetry = true) =>
        new(false, errorMessage, shouldRetry);

    public static CommandResult FailedNoRetry(string errorMessage) =>
        new(false, errorMessage, false);
}
