namespace Lucidly.Common;

public   class PropertySchema
{
    public string Type { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
}
public class StringPropertySchema : PropertySchema
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Format { get; set; } // e.g., "email", "date-time"

    public StringPropertySchema()
    {
        Type = "string";
    }
}

public class NumberPropertySchema : PropertySchema
{
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }

    public NumberPropertySchema()
    {
        Type = "number";
    }
}

public class BooleanPropertySchema : PropertySchema
{
    public bool? Default { get; set; }

    public BooleanPropertySchema()
    {
        Type = "boolean";
    }
}

public class EnumPropertySchema : PropertySchema
{
    public List<string>? Enum { get; set; }
    public List<string>? EnumNames { get; set; }

    public EnumPropertySchema()
    {
        Type = "enum";
    }
}
