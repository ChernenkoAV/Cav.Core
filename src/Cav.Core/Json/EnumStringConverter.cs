using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cav.Json;

internal class EnumStringConverter : StringEnumConverter
{
    public EnumStringConverter() => AllowIntegerValues = true;

    public override bool CanConvert(Type objectType) => base.CanConvert(objectType);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

        var values = reader.TokenType == JsonToken.StartArray
            ? serializer.Deserialize<string[]>(reader)!
            : [serializer.Deserialize<string>(reader)!];

        values = [.. values.Where(x => !x.IsNullOrWhiteSpace())];

        return values.Length == 0
            ? null
            : Enum.ToObject(enumType, values.Select(x => Enum.Parse(enumType, x)).Aggregate(0, (cur, val) => cur | (int)val));
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var isFlag = value!.GetType().GetCustomAttributes(typeof(FlagsAttribute), false).Any();

        if (isFlag)
            serializer.Serialize(writer, (value as Enum)!.FlagToList().Select(x => x.ToString()).ToArray());
        else
            base.WriteJson(writer, value, serializer);
    }
}