using Newtonsoft.Json.Serialization;

namespace Cav.Json;

internal class StringNullEmtyValueProvider(JsonProperty jsonProperty) : IValueProvider
{
    private readonly IValueProvider valueProvider = jsonProperty.ValueProvider!;
    public object? GetValue(object target)
    {
        var inst = valueProvider.GetValue(target);

        return ((string?)inst).GetNullIfIsNullOrWhiteSpace();
    }
    public void SetValue(object target, object? value)
    {
        value = ((string?)value).GetNullIfIsNullOrWhiteSpace();

        valueProvider.SetValue(target, value);
    }
}
