using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Cav.Json;

internal class JsonContractResolver : DefaultContractResolver
{
    public override JsonContract ResolveContract(Type type) => base.ResolveContract(type);
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var jProperty = base.CreateProperty(member, memberSerialization);

        var propertyType = Nullable.GetUnderlyingType(jProperty.PropertyType) ?? jProperty.PropertyType!;

        NullEnumerableValueProvider.TrySet(member, jProperty);

        if (propertyType == typeof(string))
            jProperty.ValueProvider = new StringNullEmtyValueProvider(jProperty);

        if (propertyType.IsEnum)
            jProperty.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;

        if (propertyType == typeof(DateTime))
        {
            var jsonDateTimeConverterAttrib = member.GetCustomAttribute<JsonDateTimeFormatAttribute>();
            if (jsonDateTimeConverterAttrib is not null)
            {
                jProperty.Converter = new IsoDateTimeConverter() { DateTimeFormat = jsonDateTimeConverterAttrib.DateTimeFormat };
            }
        }

        return jProperty;
    }
}
