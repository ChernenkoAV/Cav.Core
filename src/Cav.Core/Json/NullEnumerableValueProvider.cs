using System.Collections;
using Newtonsoft.Json.Serialization;

namespace Cav.Json;

internal class NullEnumerableValueProvider : IValueProvider
{
    private NullEnumerableValueProvider(IValueProvider defaultValueProvider, Type propertyType) =>
        (this.defaultValueProvider, this.propertyType) = (defaultValueProvider, propertyType);
    private IValueProvider defaultValueProvider;
    private Type propertyType;
    private Type? genericType;
    private bool isArray;
    private bool isIEnumerableGeneric;
    private bool isCreatedInumerable;

    public object? GetValue(object target)
    {
        var inst = defaultValueProvider.GetValue(target);

        return inst is null || !((IEnumerable)inst).GetEnumerator().MoveNext()
            ? null
            : inst;
    }

    public void SetValue(object target, object? value)
    {
        if (value == null)
            value = isArray || isIEnumerableGeneric ? Array.CreateInstance(genericType, 0)
            : isCreatedInumerable ? Activator.CreateInstance(propertyType) : null;

        defaultValueProvider.SetValue(target, value);
    }

    // все эти танцы - что б не попасть на типы аля string - он тож реализует IEnumerable<>
    private static Type? getGenericTypeForIEnumerableGeneric(Type type)
    {
        if (!type.IsInterface
            || !type.IsGenericType
            || typeof(IEnumerable<>).IsAssignableFrom(type))
            return null;

        var genericArguments = type.GetGenericArguments();

        return genericArguments.Length != 1 ? null : genericArguments[0];
    }

    public static NullEnumerableValueProvider? Create(JsonProperty jsonProperty)
    {
        var propertyType = jsonProperty.PropertyType!;

        var isArray = propertyType.IsArray;
        var isIEnumerable = propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType);
        var isCreatedInumerable = isIEnumerable && propertyType.GetConstructor([]) is not null;
        var genericTypeForIEnumerable = getGenericTypeForIEnumerableGeneric(propertyType);
        var isIEnumerableGeneric = genericTypeForIEnumerable is not null;

        if (!isArray && !isIEnumerable && !isCreatedInumerable && !isIEnumerableGeneric)
            return null;

        var res = new NullEnumerableValueProvider(jsonProperty.ValueProvider!, propertyType)
        {
            isArray = isArray,
            isCreatedInumerable = isCreatedInumerable,
            isIEnumerableGeneric = isIEnumerableGeneric
        };

        if (isArray)
            res.genericType = propertyType.GetElementType();

        if (isIEnumerableGeneric)
            res.genericType = genericTypeForIEnumerable;

        return res;
    }
}