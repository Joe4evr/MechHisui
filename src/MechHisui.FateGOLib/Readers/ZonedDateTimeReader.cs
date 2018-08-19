using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord.Commands;
using NodaTime;
using NodaTime.Text;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    public partial class FgoModule
    {
        private sealed class ZonedDateTimeReader : TypeReader
        {
            private static readonly AnnualDatePattern _dateReader1
                = AnnualDatePattern.CreateWithInvariantCulture("MMMdd");
            private static readonly AnnualDatePattern _dateReader2
                = AnnualDatePattern.CreateWithInvariantCulture("MMM' 'dd");
            private static readonly AnnualDatePattern _dateReader3
                = AnnualDatePattern.CreateWithInvariantCulture("MM'/'dd");
            private static readonly AnnualDatePattern _dateReader4
                = AnnualDatePattern.CreateWithInvariantCulture("dd'-'MM");
            private static readonly LocalTimePattern _timeReader
                = LocalTimePattern.CreateWithInvariantCulture("HH':'mm");

            public override Task<TypeReaderResult> ReadAsync(
                ICommandContext context,
                string input,
                IServiceProvider services)
            {
                var split = input.Split('T');

                string possibleDate = split[0].Trim();
                var dateResult = _dateReader1.Parse(possibleDate);
                dateResult = dateResult.Success ? dateResult : _dateReader2.Parse(possibleDate);
                dateResult = dateResult.Success ? dateResult : _dateReader3.Parse(possibleDate);
                dateResult = dateResult.Success ? dateResult : _dateReader4.Parse(possibleDate);

                var timeResult = _timeReader.Parse(split[1].Trim());

                return (dateResult.Success && timeResult.Success)
                    ? Task.FromResult(
                        TypeReaderResult.FromSuccess(
                            dateResult.Value.InYearOfNextOccurrance(
                                timeResult.Value,
                                NodaTimeExtensions.JpnTimeZone)))
                    : Task.FromResult(
                        TypeReaderResult.FromError(CommandError.Exception,
                            new CompositeException(dateResult.Exception, timeResult.Exception).Message));
            }
        }
    }
}
