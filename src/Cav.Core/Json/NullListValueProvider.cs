using System.Collections;
using Newtonsoft.Json.Serialization;

namespace Cav.Json;

internal class NullListValueProvider(JsonProperty jsonProperty) : IValueProvider
{
    private readonly IValueProvider valueProvider = jsonProperty.ValueProvider!;
    public object? GetValue(object target)
    {
        var inst = valueProvider!.GetValue(target);

        return inst == null
            ? null
            : !(inst as IEnumerable)!.GetEnumerator().MoveNext()
                ? null
                : inst;
    }

    public void SetValue(object target, object? value)
    {
        if (value == null)
        {
            if (jsonProperty.PropertyType!.IsArray)
                value = Array.CreateInstance(jsonProperty.PropertyType.GetElementType()!, 0);
            else
            {
                if (jsonProperty.PropertyType.GetConstructor([]) != null)
                    value = Activator.CreateInstance(jsonProperty.PropertyType);
            }
        }

        valueProvider.SetValue(target, value);
    }
}