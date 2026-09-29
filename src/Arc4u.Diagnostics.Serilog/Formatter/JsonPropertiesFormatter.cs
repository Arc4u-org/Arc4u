using Serilog.Events;
using Serilog.Formatting.Json;

namespace Arc4u.Diagnostics.Formatter;

/// <summary>
/// Build a json output of the properties added!
/// </summary>
public class JsonPropertiesFormatter
{
    /// <summary>Initializes a new instance of the <see cref="JsonPropertiesFormatter"/> class.</summary>
    public JsonPropertiesFormatter()
    {
        Formatter = new JsonValueFormatter();
    }

    private JsonValueFormatter Formatter { get; set; }

    /// <summary>Writes the properties as a single JSON object (<c>{"Name":value,...}</c>). Nothing is written when the list is empty.</summary>
    /// <param name="properties">The properties to write.</param>
    /// <param name="output">The writer that receives the JSON.</param>
    public void Format(List<LogEventProperty> properties, TextWriter output)
    {
        if (properties.Count > 0)
        {
            output.Write("{");
            var separator = string.Empty;
            foreach (var property in properties)
            {
                output.Write($"{separator}\"{property.Name}\":");
                Formatter.Format(property.Value, output);
                separator = ",";
            }
            output.Write("}");
        }
    }
}
