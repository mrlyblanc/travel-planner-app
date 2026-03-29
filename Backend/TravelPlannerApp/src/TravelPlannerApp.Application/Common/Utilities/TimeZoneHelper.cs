using TimeZoneConverter;
using TravelPlannerApp.Application.Common.Exceptions;

namespace TravelPlannerApp.Application.Common.Utilities;

public static class TimeZoneHelper
{
    public static TimeZoneInfo EnsureExists(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            throw new BadRequestException("Timezone is required.");
        }

        try
        {
            return TZConvert.GetTimeZoneInfo(timezone.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BadRequestException($"Timezone '{timezone}' is invalid.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new BadRequestException($"Timezone '{timezone}' is invalid.");
        }
    }

    public static DateTime Utc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTimeOffset ToOffset(DateTime localDateTime, string timezone)
    {
        var timeZoneInfo = EnsureExists(timezone);
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var offset = timeZoneInfo.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset);
    }
}
