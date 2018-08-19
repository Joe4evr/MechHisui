using System;
using NodaTime;

namespace SharedExtensions
{
    internal static class NodaTimeExtensions
    {
        internal static DateTimeZone JpnTimeZone { get; } = DateTimeZoneProviders.Tzdb["Japan"];

        public static ZonedDateTime InYearOfNextOccurrance(
            this AnnualDate date,
            LocalTime targetTime,
            DateTimeZone zone)
        {
            var now = SystemClock.Instance.GetCurrentInstant().InZone(zone);
            bool passedInThisYear = now.Month < date.Month || (now.Month == date.Month && now.Day < date.Day);

            return date.InYear(passedInThisYear ? now.Year : now.Year + 1)
                .At(targetTime)
                .InZoneLeniently(zone);
        }

        public static Duration TimeUntilNextOccurrance(
            this ZonedDateTime startingTime,
            AnnualDate targetDate,
            LocalTime? targetTime = null)
        {
            bool passedThisYear = startingTime.Month < targetDate.Month || (startingTime.Month == targetDate.Month && startingTime.Day < targetDate.Day);
            var dateInYear = targetDate.InYear(passedThisYear
                ? startingTime.Year
                : startingTime.Year + 1);

            var targetDateTime = (targetTime.HasValue)
                ? dateInYear.At(targetTime.Value).InZoneLeniently(startingTime.Zone)
                : dateInYear.AtStartOfDayInZone(startingTime.Zone);

            return targetDateTime - startingTime;
        }

        public static Duration TimeUntilNextOccurrance(
            this ZonedDateTime startingTime,
            LocalDateTime targetDateTime)
        {
            return targetDateTime.InZoneLeniently(startingTime.Zone) - startingTime;
        }

        public static Duration TimeUntilNextOccurrance(
            this ZonedDateTime startingTime,
            LocalTime targetTime)
        {
            var localStart = startingTime.LocalDateTime;

            var nextOccurrance = ((localStart.TimeOfDay < targetTime)
                ? localStart.Date.At(targetTime)
                : localStart.PlusDays(1).Date.At(targetTime))
                .InZoneLeniently(startingTime.Zone);

            return nextOccurrance - startingTime;
        }

        public static bool IsAnnualDate(
            this ZonedDateTime zonedDate,
            AnnualDate targetDate)
        {
            return (zonedDate.Month == targetDate.Month && zonedDate.Day == targetDate.Day);
        }
    }
}
