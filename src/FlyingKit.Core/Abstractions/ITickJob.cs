namespace FlyingKit.Core.Abstractions;

public interface ITickJob
{
    Task HandleAsync(object arguments=null);
}