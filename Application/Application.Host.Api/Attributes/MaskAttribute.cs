namespace Application.Host.Api.Attributes;

internal sealed class MaskAttribute : Attribute
{
    public MaskType Type { get; }

    public MaskAttribute(MaskType type)
    {
        Type = type;
    }
}

public enum MaskType
{
    Email,
    Name,
    Phone,
    Full
}