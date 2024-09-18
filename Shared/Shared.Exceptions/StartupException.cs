namespace Shared.Exceptions;

public class StartupException : Exception
{
    public StartupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}