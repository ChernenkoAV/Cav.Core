namespace Cav;

/// <summary>
/// Робота с коллекциями
/// </summary>
public static class StreamExt
{
    /// <summary>
    /// Чтение пототка в строку
    /// </summary>
    /// <param name="stream">Читаемый поток</param>
    /// <param name="turnToBegin">Переместиться на начало потока</param>
    /// <returns></returns>
    public static async Task<string> ReadToString(this Stream stream, bool turnToBegin = false)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (turnToBegin)
            stream.Seek(0, SeekOrigin.Begin);

        using var sr = new StreamReader(stream);
        return await sr.ReadToEndAsync();
    }
}
