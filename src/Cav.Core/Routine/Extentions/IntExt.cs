namespace Cav;

/// <summary>
/// Расширения к инту
/// </summary>
public static class IntExt
{
    /// <summary>
    /// Создание диапазона с количеством элементов
    /// </summary>
    /// <param name="count">Количество элементов</param>
    /// <param name="start">Начальный индекс</param>
    /// <returns></returns>
    public static IEnumerable<int> Range(this int count, int start = 0) =>
        count < 1 ? [] : Enumerable.Range(start, count);
}
