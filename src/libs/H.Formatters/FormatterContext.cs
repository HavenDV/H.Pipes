namespace H.Formatters;

/// <summary>
/// Contains current context with properties like PipeName.
/// </summary>
public class FormatterContext
{
    /// <summary>
    /// Contains current connection PipeName.
    /// </summary>
    public string PipeName { get; set; } = string.Empty;
}
