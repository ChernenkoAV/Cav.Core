namespace Cav;

/// <summary>
/// Робота с коллекциями
/// </summary>
public static class CollectionExt
{
    /// <summary>
    /// Соеденяет значения в коллекции с заданым разделителем
    /// </summary>
    /// <typeparam name="T">Тип идентификатора</typeparam>
    /// <param name="source">Коллекция значений</param>
    /// <param name="separator">Разделитель</param>
    /// <param name="distinct">Только уникальные значения</param>
    /// <param name="format">Формат преобразования к строке каждого объекта в коллекции(по умолчанию "{0}")</param>
    /// <returns>Значения разделенные разделителем</returns>
    public static string? JoinValuesToString<T>(
        this IEnumerable<T> source,
        string separator = ",",
        bool distinct = true,
        string? format = null)
    {
        if (source == null)
            return null;

        if (!source.Any())
            return null;

        var vals = source;
        if (distinct)
            vals = source.Distinct();

        if (!typeof(T).IsValueType)
            vals = vals.Where(x => x != null).ToArray();

        format = format.GetNullIfIsNullOrWhiteSpace() ?? "{0}";

        return string.Join(separator, vals.Select(x => string.Format(format, x)).ToArray());
    }

    /// <summary>
    /// AddRange для коллекций, в которых этого расширения(метода) нет
    /// </summary>
    /// <param name="target">Целевая коллекция</param>
    /// <param name="source">Коллекция для вставки</param>
    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        foreach (var item in source)
            target.Add(item);
    }

    /// <summary>
    /// Выполнение действия над элементами коллекции с возвратом коллекции
    /// </summary>
    /// <typeparam name="T">Тип объектов коллекции</typeparam>
    /// <param name="source">Исходная коллекция</param>
    /// <param name="action">Действие над элементом</param>
    /// <returns></returns>
    public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        foreach (var item in source)
        {
            action(item);
            yield return item;
        }
    }

    /// <summary>
    /// Выполнение действия над элементами коллекции с возвратом коллекции
    /// </summary>
    /// <typeparam name="T">Тип объектов коллекции</typeparam>
    /// <param name="source">Исходная коллекция</param>
    /// <param name="action">Действие над элементом</param>
    /// <returns></returns>
    public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        foreach (var item in source)
        {
            action(item).GetAwaiter().GetResult();
            yield return item;
        }
    }

    /// <summary>
    /// Создание элемента и добавление его в коллекцию.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции. Должен быть класс с открытым конструктором без параметров</typeparam>
    /// <param name="colection">Коллекция</param>
    /// <param name="init">Илициализатор нового экземляра элемента</param>
    /// <returns>Созданный элемент</returns>
    /// <exception cref="ArgumentNullException">коллекция = null</exception>
    public static T AddNew<T>(this ICollection<T> colection, Action<T>? init = null) where T : class, new()
    {
        if (colection is null)
            throw new ArgumentNullException(nameof(colection));

        var res = Activator.CreateInstance<T>();
        colection.Add(res);
        init?.Invoke(res);
        return res;
    }
}
