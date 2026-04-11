using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Shared.Utils;

public static class EventScheduleUtils
{
    public static EventStatus ResolveStatus(Event evt, DateTime nowUtc)
    {
        return TryGetCurrentOccurrence(evt, nowUtc, out _, out _)
            ? EventStatus.Ongoing
            : TryGetNextOccurrence(evt, nowUtc, out _, out _)
                ? EventStatus.Upcoming
                : EventStatus.Ended;
    }

    public static bool TryGetCurrentOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;

        return evt.ScheduleType switch
        {
            ScheduleType.ExactDate => TryGetCurrentExactOccurrence(evt, nowUtc, out startUtc, out endUtc),
            ScheduleType.YearlyRecurring => TryGetCurrentYearlyOccurrence(evt, nowUtc, out startUtc, out endUtc),
            ScheduleType.MonthlyRecurring => TryGetCurrentMonthlyOccurrence(evt, nowUtc, out startUtc, out endUtc),
            _ => false
        };
    }

    public static bool TryGetNextOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;

        return evt.ScheduleType switch
        {
            ScheduleType.ExactDate => TryGetNextExactOccurrence(evt, nowUtc, out startUtc, out endUtc),
            ScheduleType.YearlyRecurring => TryGetNextYearlyOccurrence(evt, nowUtc, out startUtc, out endUtc),
            ScheduleType.MonthlyRecurring => TryGetNextMonthlyOccurrence(evt, nowUtc, out startUtc, out endUtc),
            _ => false
        };
    }

    public static List<(DateTime StartUtc, DateTime EndUtc)> ResolveOccurrences(Event evt, DateTime fromUtc, DateTime toUtc)
    {
        if (toUtc < fromUtc)
            return [];

        return evt.ScheduleType switch
        {
            ScheduleType.ExactDate => ResolveExactOccurrences(evt, fromUtc, toUtc),
            ScheduleType.YearlyRecurring => ResolveYearlyOccurrences(evt, fromUtc, toUtc),
            ScheduleType.MonthlyRecurring => ResolveMonthlyOccurrences(evt, fromUtc, toUtc),
            _ => []
        };
    }

    private static bool TryGetCurrentExactOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = evt.StartAt.HasValue ? ToUtc(evt.StartAt.Value) : default;
        endUtc = evt.EndAt.HasValue ? ToUtc(evt.EndAt.Value) : default;
        return evt.StartAt.HasValue && evt.EndAt.HasValue && startUtc <= nowUtc && endUtc >= nowUtc;
    }

    private static bool TryGetNextExactOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = evt.StartAt.HasValue ? ToUtc(evt.StartAt.Value) : default;
        endUtc = evt.EndAt.HasValue ? ToUtc(evt.EndAt.Value) : default;
        return evt.StartAt.HasValue && evt.EndAt.HasValue && startUtc > nowUtc;
    }

    private static bool TryGetCurrentYearlyOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;
        if (!HasYearlyFields(evt))
            return false;

        foreach (var year in new[] { nowUtc.Year - 1, nowUtc.Year })
        {
            if (!TryBuildYearlyOccurrence(evt, year, out var start, out var end))
                continue;

            if (start <= nowUtc && nowUtc <= end)
            {
                startUtc = start;
                endUtc = end;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNextYearlyOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;
        if (!HasYearlyFields(evt))
            return false;

        foreach (var year in new[] { nowUtc.Year, nowUtc.Year + 1, nowUtc.Year + 2 })
        {
            if (!TryBuildYearlyOccurrence(evt, year, out var start, out var end))
                continue;

            if (start > nowUtc)
            {
                startUtc = start;
                endUtc = end;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetCurrentMonthlyOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;
        if (!HasMonthlyFields(evt))
            return false;

        var previous = nowUtc.AddMonths(-1);
        foreach (var (year, month) in new[] { (previous.Year, previous.Month), (nowUtc.Year, nowUtc.Month) })
        {
            if (!TryBuildMonthlyOccurrence(evt, year, month, out var start, out var end))
                continue;

            if (start <= nowUtc && nowUtc <= end)
            {
                startUtc = start;
                endUtc = end;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNextMonthlyOccurrence(Event evt, DateTime nowUtc, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;
        if (!HasMonthlyFields(evt))
            return false;

        var year = nowUtc.Year;
        var month = nowUtc.Month;

        for (var i = 0; i < 24; i++)
        {
            if (TryBuildMonthlyOccurrence(evt, year, month, out var start, out var end) && start > nowUtc)
            {
                startUtc = start;
                endUtc = end;
                return true;
            }

            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }
        }

        return false;
    }

    private static List<(DateTime StartUtc, DateTime EndUtc)> ResolveExactOccurrences(Event evt, DateTime fromUtc, DateTime toUtc)
    {
        if (!evt.StartAt.HasValue || !evt.EndAt.HasValue)
            return [];

        var start = ToUtc(evt.StartAt.Value);
        var end = ToUtc(evt.EndAt.Value);

        if (end < fromUtc || start > toUtc)
            return [];

        return [(start, end)];
    }

    private static List<(DateTime StartUtc, DateTime EndUtc)> ResolveYearlyOccurrences(Event evt, DateTime fromUtc, DateTime toUtc)
    {
        var result = new List<(DateTime StartUtc, DateTime EndUtc)>();
        if (!HasYearlyFields(evt))
            return result;

        for (var year = fromUtc.Year - 1; year <= toUtc.Year + 1; year++)
        {
            if (!TryBuildYearlyOccurrence(evt, year, out var start, out var end))
                continue;

            if (end < fromUtc || start > toUtc)
                continue;

            result.Add((start, end));
        }

        return result.OrderBy(x => x.StartUtc).ToList();
    }

    private static List<(DateTime StartUtc, DateTime EndUtc)> ResolveMonthlyOccurrences(Event evt, DateTime fromUtc, DateTime toUtc)
    {
        var result = new List<(DateTime StartUtc, DateTime EndUtc)>();
        if (!HasMonthlyFields(evt))
            return result;

        var cursor = new DateTime(fromUtc.Year, fromUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        var limit = new DateTime(toUtc.Year, toUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        while (cursor <= limit)
        {
            if (TryBuildMonthlyOccurrence(evt, cursor.Year, cursor.Month, out var start, out var end))
            {
                if (!(end < fromUtc || start > toUtc))
                    result.Add((start, end));
            }

            cursor = cursor.AddMonths(1);
        }

        return result.OrderBy(x => x.StartUtc).ToList();
    }

    private static bool HasYearlyFields(Event evt)
        => evt.StartMonth.HasValue && evt.StartDay.HasValue && evt.EndMonth.HasValue && evt.EndDay.HasValue;

    private static bool HasMonthlyFields(Event evt)
        => evt.StartDay.HasValue && evt.EndDay.HasValue;

    private static bool TryBuildYearlyOccurrence(Event evt, int year, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;

        if (!HasYearlyFields(evt))
            return false;

        if (!TryCreateDate(year, evt.StartMonth!.Value, evt.StartDay!.Value, out startUtc))
            return false;

        var endYear = year;
        if (IsYearlyCrossYear(evt.StartMonth.Value, evt.StartDay.Value, evt.EndMonth!.Value, evt.EndDay!.Value))
            endYear = year + 1;

        if (!TryCreateDate(endYear, evt.EndMonth.Value, evt.EndDay.Value, out endUtc))
            return false;

        endUtc = EndOfDay(endUtc);
        return endUtc >= startUtc;
    }

    private static bool TryBuildMonthlyOccurrence(Event evt, int year, int month, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;

        if (!HasMonthlyFields(evt))
            return false;

        var startDay = Math.Min(evt.StartDay!.Value, DateTime.DaysInMonth(year, month));
        startUtc = new DateTime(year, month, startDay, 0, 0, 0, DateTimeKind.Utc);

        if (evt.EndDay!.Value >= evt.StartDay.Value)
        {
            var endDaySameMonth = Math.Min(evt.EndDay.Value, DateTime.DaysInMonth(year, month));
            endUtc = EndOfDay(new DateTime(year, month, endDaySameMonth, 0, 0, 0, DateTimeKind.Utc));
        }
        else
        {
            var next = startUtc.AddMonths(1);
            var endDayNextMonth = Math.Min(evt.EndDay.Value, DateTime.DaysInMonth(next.Year, next.Month));
            endUtc = EndOfDay(new DateTime(next.Year, next.Month, endDayNextMonth, 0, 0, 0, DateTimeKind.Utc));
        }

        return endUtc >= startUtc;
    }

    private static bool IsYearlyCrossYear(int startMonth, int startDay, int endMonth, int endDay)
        => endMonth < startMonth || (endMonth == startMonth && endDay < startDay);

    private static bool TryCreateDate(int year, int month, int day, out DateTime value)
    {
        value = default;
        if (month is < 1 or > 12)
            return false;
        if (day < 1)
            return false;

        var maxDay = DateTime.DaysInMonth(year, month);
        if (day > maxDay)
            return false;

        value = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        return true;
    }

    private static DateTime EndOfDay(DateTime dateUtc)
        => new(dateUtc.Year, dateUtc.Month, dateUtc.Day, 23, 59, 59, DateTimeKind.Utc);

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
