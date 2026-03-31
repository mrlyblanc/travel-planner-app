using System.Globalization;
using System.Security.Cryptography;

namespace TravelPlannerApp.Application.Common.Utilities;

public static class ShareCodeGenerator
{
    public static string NewFiveDigitCode()
    {
        var value = RandomNumberGenerator.GetInt32(10000, 100000);
        return value.ToString("D5", CultureInfo.InvariantCulture);
    }
}
