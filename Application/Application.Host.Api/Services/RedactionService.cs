using Application.Host.Api.Attributes;
using MassTransit.Internals;

namespace Application.Host.Api.Services;

public class RedactionService : IRedactionService
{
    public T Redact<T>(T model)
    {
        if (model == null)
            return model;

        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            if (Attribute.IsDefined(property, typeof(RedactAttribute)))
            {
                if (property.CanWrite)
                {
                    property.SetValue(
                        model, 
                        property.PropertyType.CanBeNull() 
                            ? null 
                            : GetDefault(property.PropertyType)
                    );
                }
            }
            else if (Attribute.IsDefined(property, typeof(MaskAttribute)))
            {
                var maskAttribute = (MaskAttribute?)Attribute.GetCustomAttribute(property, typeof(MaskAttribute));
                if (maskAttribute != null && property.CanWrite)
                {
                    var value = property.GetValue(model)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        property.SetValue(model, MaskValue(value, maskAttribute.Type));
                    }
                }
            }
        }

        return model;
    }

    private object? GetDefault(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }

    private string MaskValue(string value, MaskType type)
    {
        // TODO Add name and phone mask types
        return type switch
        {
            MaskType.Email => MaskEmail(value),
            _ => new string('*', value.Length),
        };
    }

    private string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex < 1)
            return email;

        var localPart = email.Substring(0, atIndex);
        var domainPart = email.Substring(atIndex);

        return localPart.Length <= 2 
            ? $"{localPart[0]}*{domainPart}" 
            : $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}{domainPart}";
    }
}