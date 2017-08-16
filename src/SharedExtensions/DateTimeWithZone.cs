using System;

namespace SharedExtensions
{
    /// <summary>
    /// Represents a <see cref="DateTime"/> inside a particular timezone.
    /// </summary>
    /// <remarks>Shamelessly stolen (then slightly modified) from Jon Skeet's SO answer @ http://stackoverflow.com/a/246529 </remarks>
    public struct DateTimeWithZone
    {
        /// <summary>
        /// The specified time in UTC.
        /// </summary>
        public DateTime UniversalTime { get; }

        /// <summary>
        /// The specified <see cref="TimeZoneInfo"/>.
        /// </summary>
        public TimeZoneInfo TimeZone { get; }

        /// <summary>
        /// The specified time local to the specified timezone.
        /// </summary>
        public DateTime LocalTime => TimeZoneInfo.ConvertTime(UniversalTime, TimeZone);

        /// <summary>
        /// Creates a <see cref="DateTime"/> inside a particular timezone.
        /// </summary>
        /// <param name="dateTimeUtc">The current <see cref="DateTime"/> in UTC.</param>
        /// <param name="timeZone">The <see cref="TimeZoneInfo"/> of the desired Timezone.</param>
        public DateTimeWithZone(DateTime dateTimeUtc, TimeZoneInfo timeZone)
        {
            UniversalTime = dateTimeUtc;
            TimeZone = timeZone;
        }

        /// <summary>
        /// Calculates the amount of time left until the specified time in the specified timezone.
        /// </summary>
        /// <param name="targetTimeOfDay">A <see cref="TimeSpan"/> of the desired time of day.</param>
        /// <returns>A <see cref="TimeSpan"/> of the remaining time.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Parameter was more than 24 hours.</exception>
        public TimeSpan TimeUntilNextLocalTimeAt(TimeSpan targetTimeOfDay)
        {
            if (targetTimeOfDay > TimeSpan.FromDays(1))
            {
                throw new ArgumentOutOfRangeException(nameof(targetTimeOfDay), "Parameter value may not exceed 24 hours.");
            }

            return (LocalTime.TimeOfDay > targetTimeOfDay) ?
                TimeSpan.FromDays(1) - (LocalTime.TimeOfDay - targetTimeOfDay) :
                targetTimeOfDay - LocalTime.TimeOfDay;
        }
    }
}
