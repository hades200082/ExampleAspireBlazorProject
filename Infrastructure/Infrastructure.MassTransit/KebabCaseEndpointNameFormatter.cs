using System.Text.RegularExpressions;
using MassTransit;

namespace Infrastructure.MassTransit;

public class KebabCaseEndpointNameFormatter : IEndpointNameFormatter, IEntityNameFormatter
{
    public string Separator => "-";

    private string KebabCase(string input) => Regex.Replace(input, @"([a-z])([A-Z])", "$1-$2").ToLower();

    public string SanitizeName(string name)
    {
        return Regex.Replace(KebabCase(name), @"[^a-z0-9]+", Separator);
    }

    public string TemporaryEndpoint(string tag)
    {
        return SanitizeName($"temporary-{tag}");
    }

    public string Consumer<T>() where T : class, IConsumer
    {
        return SanitizeName(typeof(T).Name.Replace("Consumer", ""));
    }

    public string Saga<T>() where T : class, ISaga
    {
        return SanitizeName(typeof(T).Name.Replace("Saga", ""));
    }

    public string ExecuteActivity<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return SanitizeName($"{typeof(T).Name.Replace("Activity", "")}-execute");
    }

    public string CompensateActivity<T, TLog>()
        where T : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return SanitizeName($"{typeof(T).Name.Replace("Activity", "")}-compensate");
    }

    public string Message<T>() where T : class
    {
        return SanitizeName($"topic-{typeof(T).Name}");
    }

    public string SagaMessage<TSaga, T>()
        where TSaga : class, ISaga
        where T : class
    {
        return SanitizeName($"topic-{typeof(TSaga).Name.Replace("Saga", "")}-{typeof(T).Name}");
    }

    public string Generate<T>() where T : class
    {
        return SanitizeName(typeof(T).Name);
    }

    public string FormatEntityName<T>()
    {
        return SanitizeName($"topic-{typeof(T).Name}");
    }
}
