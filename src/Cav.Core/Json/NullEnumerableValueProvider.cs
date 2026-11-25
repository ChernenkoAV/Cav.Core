using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cav.Json;

internal class NullEnumerableValueProvider(IValueProvider defaultValueProvider, Type genericType) : IValueProvider
{
    private Func<object?> createDefault = () => null;
    private Func<object?> createArray = () => Array.CreateInstance(genericType, 0);
    private Func<object?> createIEnumerable = () => Activator.CreateInstance(genericType);

    public object? GetValue(object target)
    {
        var inst = defaultValueProvider.GetValue(target);

        return inst is null || !((IEnumerable)inst).GetEnumerator().MoveNext()
            ? null
            : inst;
    }

    public void SetValue(object target, object? value)
    {
        if (value is null)
            value = createDefault();

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

        return genericArguments.Length == 1 ? genericArguments[0] : null;
    }

    public static void TrySet(MemberInfo member, JsonProperty jsonProperty)
    {
        var propertyType = jsonProperty.PropertyType!;

        var isArray = propertyType.IsArray;
        var isIEnumerable = propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType);
        var isCreatedInumerable = isIEnumerable && propertyType.GetConstructor([]) is not null;
        var genericTypeForIEnumerable = getGenericTypeForIEnumerableGeneric(propertyType);
        var isIEnumerableGeneric = genericTypeForIEnumerable is not null;
        var isNullableProperty = false;

        if (!isArray && !isIEnumerable && !isCreatedInumerable && !isIEnumerableGeneric)
            return;

        var genType = propertyType;

        if (isArray || isIEnumerableGeneric)
            genType = genericTypeForIEnumerable ?? propertyType.GetElementType();

        var valProvider = new NullEnumerableValueProvider(jsonProperty.ValueProvider!, genType);

        // =====
        // пробуем поискать nullable для проперти
        const string compilerServicesNameSpace = "System.Runtime.CompilerServices";
        const string nullableAttribute = "NullableAttribute";
        const string nullableContextAttribute = "NullableContextAttribute";

        var attribData = member.GetCustomAttributesData()
            .FirstOrDefault(x =>
                x.AttributeType.Namespace == compilerServicesNameSpace
                && x.AttributeType.Name == nullableAttribute);

        while (attribData is null && member is not null)
        {
            attribData = member.GetCustomAttributesData()
                .FirstOrDefault(x =>
                    x.AttributeType.Namespace == compilerServicesNameSpace
                    && x.AttributeType.Name == nullableContextAttribute);

            if (attribData is null)
                member = member.DeclaringType;
        }

        if (attribData is not null && attribData.ConstructorArguments.Count == 1)
        {
            var arg = attribData.ConstructorArguments[0].Value;
            isNullableProperty =
                (arg is byte b && b == 2)
                || (arg is ReadOnlyCollection<CustomAttributeTypedArgument> ba && ba.Count > 0 && ba[0].Value is byte bba && bba == 2);
        }

        // ==================

        if (!isNullableProperty)
        {
            if (isArray || isIEnumerableGeneric)
                valProvider.createDefault = valProvider.createArray;

            if (isCreatedInumerable)
                valProvider.createDefault = valProvider.createIEnumerable;
        }

        jsonProperty.ValueProvider = valProvider;

        var olsShouldSerialise = jsonProperty.ShouldSerialize ?? (x => true);

        jsonProperty.ShouldSerialize = obj => olsShouldSerialise(obj) && jsonProperty.ValueProvider!.GetValue(obj) != null;

        jsonProperty.NullValueHandling = NullValueHandling.Include;
        jsonProperty.DefaultValueHandling = DefaultValueHandling.Populate;
    }
}