namespace Cav;

/// <summary>
/// Конвертирование <see cref="DateTime"/> по фомату. По умолчанию "yyyy-MM-dd"
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class JsonDateTimeFormatAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public JsonDateTimeFormatAttribute() { }

    /// <summary>
    /// 
    /// </summary>
    public JsonDateTimeFormatAttribute(string dateTimeFormat) => DateTimeFormat = dateTimeFormat.ThrowIfNull();

    public string DateTimeFormat { get; set; } = "yyyy-MM-dd";
}
