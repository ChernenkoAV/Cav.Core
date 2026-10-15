using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Cav.Json;

/// <summary>
/// Общая настройка сериализатора Json
/// </summary>
public class JsonSerializerSetting : JsonSerializerSettings
{
#pragma warning disable IDE0032 // Использовать свойство auto
    private static JsonSerializerSetting? instance;
#pragma warning restore IDE0032 // Использовать свойство auto

    /// <summary>
    /// Получение экземпляра настроек
    /// </summary>
    public static JsonSerializerSetting Instance => instance ??= new();

    internal JsonSerializerSetting(
#pragma warning disable SYSLIB0050 // Тип или член устарел
        StreamingContextStates state,
        object? additional) : this() =>
        Context = new StreamingContext(state, additional);
#pragma warning restore SYSLIB0050 // Тип или член устарел

    private JsonSerializerSetting()
    {
        NullValueHandling = NullValueHandling.Ignore;
        DefaultValueHandling = DefaultValueHandling.Ignore;
        Converters.Add(new EnumStringConverter());
        ContractResolver = new JsonContractResolver();
    }
}