using System.Diagnostics;

namespace Cav;

public static class TelemetryExt
{
    /// <summary>
    /// Запуск новой "внутренней" <see cref="ActivityKind.Internal"/> активности
    /// </summary>
    /// <param name="activitySource"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Activity? StartNewActivity(this ActivitySource activitySource, string name) =>
           activitySource.ThrowIfNull().StartActivity(name, ActivityKind.Internal, null);
}
