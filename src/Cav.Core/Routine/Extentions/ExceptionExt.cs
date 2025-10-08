using System.Reflection;
using System.Text;

namespace Cav;

/// <summary>
/// Расширения для исключений
/// </summary>
public static class ExceptionExt
{
    private const string padder = "-----";
    /// <summary>
    /// Развертывание текста исключения
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="refinedDecoding">приоритетная дополнительная логика раскодирования исключения</param>
    /// <returns></returns>
    public static string Expand(this Exception ex, Func<Exception, string>? refinedDecoding = null)
    {
        var builder = new StringBuilder();
        if (ex == null)
            return string.Empty;

        builder.AppendLine(ex.Message);

        if (ex.Data.Count > 0)
            foreach (var key in ex.Data.Keys)
                builder.AppendLine($"{key}: {ex.Data[key]}");

        if (refinedDecoding != null)
            builder.AppendLine(refinedDecoding(ex));

        builder.AppendLine($"Type: {ex.GetType().FullName}");

        if (!ex.StackTrace.IsNullOrWhiteSpace())
            builder.AppendLine($"StackTrace: {ex.StackTrace}");

        if (ex is ReflectionTypeLoadException reflectEx && reflectEx.LoaderExceptions != null)
        {
            builder.AppendLine(padder);
            builder.AppendLine("LoaderExceptions:");
            foreach (var rEx in reflectEx.LoaderExceptions.Where(x => x != null))
                builder.AppendLine(rEx!.Expand(refinedDecoding));
        }

        if (ex.InnerException != null)
        {
            builder.AppendLine(padder);
            builder.AppendLine("InnerException:");
            builder.AppendLine(ex.InnerException.Expand(refinedDecoding));
        }

        if (ex is AggregateException agrEx && agrEx.InnerExceptions != null)
            foreach (var inEx in agrEx.InnerExceptions)
            {
                builder.AppendLine(padder);
                builder.AppendLine("InnerException:");
                builder.AppendLine(inEx.Expand(refinedDecoding));
            }

        return builder.ToString();
    }

    /// <summary>
    /// Получение словаря <see cref="Exception.Data"/> в виде перечесления пар ключ-значение
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static Dictionary<string, object?> GetData(this Exception ex)
    {
        if (ex is null)
            return [];

        var res = new Dictionary<string, object?>();

        foreach (var item in ex.Data.Keys)
            res.Add(item!.ToString()!, ex.Data[item]);

        return res;
    }

    /// <summary>
    /// Добавление к исключению данных в словарь <see cref="Exception.Data"/>.<br/>
    /// При наличии в словаре указанного ключа значение перезаписывается.<br/>
    /// Если ключ <see cref="StringExt.IsNullOrWhiteSpace(string?)"/> = <keyword>true</keyword>, то добавление не производится.<br/>
    /// Если значение <see cref="StringExt.IsNullOrWhiteSpace(string?)"/> = <keyword>true</keyword>, то добавление не производится.<br/>
    /// Если значение <see cref="string"/> или <see cref="Guid"/> то берется <see cref="object.ToString"/>, иначе <see cref="JsonExt.JsonSerialize(object?)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ex">Исключение, в которое добавляются данные</param>
    /// <param name="key">Строковое представление ключа</param>
    /// <param name="val">Объект значения</param>
    public static void DataAdd<T>(this Exception ex, string? key, T? val)
    {
        if (ex is null || key.IsNullOrWhiteSpace() || val is null)
            return;

        var valStr = val switch
        {
            string s => s,
            Guid g => g.ToString(),
            _ => val.JsonSerialize()
        };

        if (valStr.IsNullOrWhiteSpace())
            return;

        if (ex.Data.Contains(key!))
            ex.Data.Remove(key!);
        ex.Data.Add(key!, valStr);
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Добавление к исключению данных в словарь <see cref="Exception.Data"/>.
    /// Для ключа используется имя члена, переданной в метод компилятором (если не указано вручную).
    /// </summary>
    /// <param name="ex">Исключение, в которое добавляются данные</param>
    /// <param name="val">Объект значения</param>
    /// <param name="argumentName">Имя члена</param>
    public static void DataAdd(this Exception ex, object? val, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(val))] string argumentName = "") => ex.DataAdd(argumentName, val);
#endif
}
