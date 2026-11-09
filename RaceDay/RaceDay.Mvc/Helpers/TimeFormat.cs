namespace RaceDay.Mvc.Helpers;

public static class TimeFormat
{
    public static string Hms(int totalSeconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(totalSeconds, 0));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }
}