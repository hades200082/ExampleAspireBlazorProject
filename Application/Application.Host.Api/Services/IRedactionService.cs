namespace Application.Host.Api.Services;

public interface IRedactionService
{
    T Redact<T>(T model);
}