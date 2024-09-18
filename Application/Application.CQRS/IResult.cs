using System.Diagnostics.CodeAnalysis;

namespace Application.CQRS;

public interface IResult
{
    bool IsSuccess { get; }
    string? ErrorCode { get; }
    string? ErrorMessage { get; }
}

public interface IObjectResult<out T> : IResult
    where T : class
{
    T? Object { get; }
}

public interface IValueResult<out T> : IResult
    where T : IComparable
{
    public T? Value { get; }
}



public record Result : IResult
{
    protected Result([NotNull]string errorCode, [NotNull]string errorMessage)
    {
        IsSuccess = false;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    protected Result()
    {
        IsSuccess = true;
    }
    
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
}

public sealed record ObjectResult<T> : Result, IObjectResult<T>
    where T : class
{
    public ObjectResult(string errorCode, string errorMessage)
        : base(errorCode, errorMessage)
    {
    }
    
    public ObjectResult(T @object)
    {
        Object = @object;
    }

    public T? Object { get; }
}

public sealed record ValueResult<T> : Result, IValueResult<T>
    where T : IComparable
{
    public ValueResult(string errorCode, string errorMessage)
        : base(errorCode, errorMessage)
    {
    }
    
    public ValueResult(T value)
    {
        Value = value;
    }

    public T? Value { get; }
}