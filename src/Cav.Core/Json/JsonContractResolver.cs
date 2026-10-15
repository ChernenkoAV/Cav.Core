using System.Collections;
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

        if (jProperty.PropertyType!.IsArray ||
            (typeof(IEnumerable).IsAssignableFrom(jProperty.PropertyType) &&
            jProperty.PropertyType.GetConstructor([]) != null))
        {
            var olsShouldSerialise = jProperty.ShouldSerialize ?? (x => true);

            jProperty.ShouldSerialize = obj => olsShouldSerialise(obj) && jProperty.ValueProvider!.GetValue(obj) != null;

            jProperty.ValueProvider = new NullListValueProvider(jProperty);
            jProperty.NullValueHandling = NullValueHandling.Include;
            jProperty.DefaultValueHandling = DefaultValueHandling.Populate;
        }

        if (jProperty.PropertyType == typeof(string))
            jProperty.ValueProvider = new StringNullEmtyValueProvider(jProperty);

        if (jProperty.PropertyType.IsEnum)
            jProperty.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;

        if (jProperty.PropertyType == typeof(DateTime))
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
