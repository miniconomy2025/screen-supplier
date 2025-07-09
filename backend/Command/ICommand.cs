namespace ScreenProducerAPI.Commands;

public interface ICommand<TResult>
{
    Task<TResult> ExecuteAsync();
}