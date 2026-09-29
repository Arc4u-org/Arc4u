using System.Globalization;
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.Dependency
{
    /// <summary>
    /// Extension methods to initialize the <see cref="TimeZoneContext"/>.
    /// </summary>
    public static class TimeZoneContextContainerExtension
    {
        /// <summary>
        /// Initializes the <see cref="TimeZoneContext"/> by resolving it from the service provider, which makes <see cref="TimeZoneContext.Current"/> available.
        /// The <see cref="ApplicationConfig"/> options must be registered (see <c>AddApplicationConfig</c>).
        /// </summary>
        /// <param name="app">The service provider.</param>
        /// <example>
        /// <code language="csharp">
        /// var app = builder.Build();
        /// app.Services.InitializeTimeZoneContext();
        /// </code>
        /// </example>
        public static void InitializeTimeZoneContext(this IServiceProvider app)
        {
            app.GetRequiredService<TimeZoneContext>();
        }

    }
}

namespace Arc4u
{
    /// <summary>
    /// Converts dates between UTC and the time zone of the application, which is defined by <see cref="Arc4u.Configuration.Environment.TimeZone"/>
    /// in the <see cref="ApplicationConfig"/>. When the time zone is not configured or cannot be found, the time zone of the machine is used.
    /// The most recently created instance is available through <see cref="Current"/>.
    /// </summary>
    [Export, Shared]
    public class TimeZoneContext
    {
        internal TimeZoneInfo _timeZone;

        //internal bool _isSameTimeZoneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeZoneContext"/> class and makes it the <see cref="Current"/> context.
        /// </summary>
        /// <param name="config">The application configuration that defines the time zone.</param>
        /// <param name="logger">The logger used to trace the time zone selection and errors.</param>
        public TimeZoneContext(IOptionsMonitor<ApplicationConfig> config, ILogger<TimeZoneContext> logger)
        {
            _timeZone = TimeZoneInfo.Local;
            IntializeFromConfig(config.CurrentValue, logger);
            _current = this;
        }

        private static TimeZoneContext? _current;
        /// <summary>
        /// Gets the most recently created <see cref="TimeZoneContext"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">No <see cref="TimeZoneContext"/> has been created yet.</exception>
        public static TimeZoneContext Current => _current ?? throw new InvalidOperationException("No timezone context is defined");

        private void IntializeFromConfig(ApplicationConfig config, ILogger<TimeZoneContext> logger)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(logger);

            try
            {
                logger.Technical().LogTryToUseTimeZone(config.Environment.TimeZone);

                if (!string.IsNullOrWhiteSpace(config.Environment.TimeZone))
                {
                    _timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.Environment.TimeZone);
                }
            }
            catch (Exception ex)
            {
                logger.Technical().LogZoneNotFound(config.Environment.TimeZone);
                logger.Technical().LogException(ex);
            }
        }

        /// <summary>
        /// Gets the time zone used by the context.
        /// </summary>
        public TimeZoneInfo TimeZoneInfo
        {
            get
            {
                return _timeZone;
            }
        }

#if !WINDOWS_UAP
        /// <summary>
        /// Gets the start and end of the daylight saving time of the time zone for a year.
        /// </summary>
        /// <param name="inYear">The year.</param>
        /// <returns>The daylight saving period, or <see langword="null"/> when the time zone has no adjustment rule for that year.</returns>
        public DaylightTime? GetDaylightChanges(int inYear)
        {
            var adjustments = TimeZoneInfo.GetAdjustmentRules();
            if (adjustments.Length == 0)
            {
                return null; // No Daylighttime.
            }
            //Find the correct adjustment rule
            var ruleFound = adjustments.SingleOrDefault(a => a.DateStart.Year <= inYear && a.DateEnd.Year >= inYear);

            if (null == ruleFound)
            {
                return null;
            }

            var outDaylightTime = new DaylightTime(GetDateTime(inYear, ruleFound.DaylightTransitionStart),
                                                            GetDateTime(inYear, ruleFound.DaylightTransitionEnd),
                                                            ruleFound.DaylightDelta);

            return outDaylightTime;
        }

        private static DateTime GetDateTime(int year, TimeZoneInfo.TransitionTime transition)
        {
            // For non-fixed date rules, get local calendar
            Calendar cal = new GregorianCalendar();
            // Get first day of week for transition
            // For example, the 3rd week starts no earlier than the 15th of the month
            var startOfWeek = transition.Week * 7 - 6;
            // What day of the week does the month start on?
            var firstDayOfWeek = (int)cal.GetDayOfWeek(new DateTime(year, transition.Month, 1));
            // Determine how much start date has to be adjusted
            int transitionDay;
            var changeDayOfWeek = (int)transition.DayOfWeek;

            if (firstDayOfWeek <= changeDayOfWeek)
            {
                transitionDay = startOfWeek + (changeDayOfWeek - firstDayOfWeek);
            }
            else
            {
                transitionDay = startOfWeek + (7 - firstDayOfWeek + changeDayOfWeek);
            }

            // Adjust for months with no fifth week
            if (transitionDay > cal.GetDaysInMonth(year, transition.Month))
            {
                transitionDay -= 7;
            }

            return new DateTime(year, transition.Month, transitionDay).AsLocalTime() + transition.TimeOfDay.TimeOfDay;

        }

        /// <summary>
        /// Gets the week number of a date, using the calendar week rule and the first day of the week of the current culture.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The week number in the year.</returns>
        public static int GetWeekNumber(DateTime date)
        {
            var culture = CultureInfo.CurrentCulture;

            return culture.Calendar.GetWeekOfYear(date,
                culture.DateTimeFormat.CalendarWeekRule,
                culture.DateTimeFormat.FirstDayOfWeek);
        }
#endif

        /// <summary>
        /// Converts a UTC date to the time zone of the context.
        /// </summary>
        /// <param name="value">The date, whose <see cref="DateTime.Kind"/> must be <see cref="DateTimeKind.Utc"/>.</param>
        /// <returns>The date in the time zone of the context, with a <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Local"/>.</returns>
        /// <exception cref="InvalidTimeZoneException"><paramref name="value"/> is not a UTC date.</exception>
        public DateTime ConvertFromUtc(DateTime value)
        {
            if (DateTimeKind.Utc != value.Kind)
            {
                throw new InvalidTimeZoneException("An Utc date is mandatory!");
            }

            var date = TimeZoneInfo.ConvertTimeFromUtc(value, _timeZone);
            return DateTime.SpecifyKind(date, DateTimeKind.Local);
        }

        /// <summary>
        /// Converts a date expressed in the time zone of the context to UTC.
        /// </summary>
        /// <param name="value">The date. A date of kind <see cref="DateTimeKind.Utc"/> is returned unchanged; otherwise it is considered to be in the time zone of the context.</param>
        /// <returns>The UTC date.</returns>
        public DateTime ConvertToUtc(DateTime value)
        {
            if (DateTimeKind.Utc == value.Kind)
            {
                return value;
            }

            var unspecifiedDate = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTime(unspecifiedDate, _timeZone, TimeZoneInfo.Utc);
        }
    }
}
