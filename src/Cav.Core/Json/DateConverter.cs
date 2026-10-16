using Newtonsoft.Json.Converters;

namespace Cav.Json;

/// <summary>
/// Конвертирование <see cref="DateTime"/> по фомату. По умолчанию "yyyy-MM-dd". Для указания своего формата используйте параметризированный конструктор
/// </summary>
[Obsolete("Используйте Cav.JsonDateConverter")]
public class DateConverter : IsoDateTimeConverter
{
    /// <summary>
    /// 
    /// </summary>
    public DateConverter() : this("yyyy-MM-dd") { }

    /// <summary>
    /// 
    /// </summary>
    public DateConverter(string dateTimeFormat) => DateTimeFormat = dateTimeFormat;
}
