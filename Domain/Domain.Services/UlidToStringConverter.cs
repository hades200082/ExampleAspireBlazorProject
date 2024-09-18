using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class UlidToStringConverter : ValueConverter<Ulid, string>
{
    private static readonly ConverterMappingHints DefaultHints = new ConverterMappingHints(size: 26);
    
    // Parameterless constructor
    public UlidToStringConverter()
        : this(null)
    {
    }

    public UlidToStringConverter(ConverterMappingHints? mappingHints = null) : base(
        convertToProviderExpression: x => x.ToString(),
        convertFromProviderExpression: x => Ulid.Parse(x),
        mappingHints: DefaultHints.With(mappingHints))
    {
    }
}