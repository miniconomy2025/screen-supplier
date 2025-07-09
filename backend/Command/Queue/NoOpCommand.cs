namespace ScreenProducerAPI.Commands.Queue;

public class NoOpCommand : ICommand<CommandResult>
{
    private readonly string _status;

    public NoOpCommand(string status)
    {
        _status = status;
    }

    public Task<CommandResult> ExecuteAsync()
    {
        // For terminal states or states that don't need processing
        return Task.FromResult(CommandResult.Succeeded());
    }
}