
public class PropertyMissingException : Exception
{
    public PropertyMissingException(string parameter) : base($"Missing property: {parameter}") { }
}
