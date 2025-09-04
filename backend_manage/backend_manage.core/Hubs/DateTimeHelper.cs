using System.Runtime.InteropServices;

namespace backend_manage.core.Hubs;

public static class DateTimeHelper
{
    public static DateTime GetVietnamTime()
    {
        string tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh";

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
    }
}
