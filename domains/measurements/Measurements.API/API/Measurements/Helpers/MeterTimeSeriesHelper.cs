using System;
using System.Globalization;
using Measurements.V1;

namespace API.Measurements.Helpers
{
    public static class MeterTimeSeriesHelper
    {
        private const string TimeZoneId = "Romance Standard Time";
        public const string HourlyMeasurementsOccurrence = "PT1H";
        public const string QuarterlyMeasurementsOccurrence = "PT15M";

        public static DateTimeOffset ConvertDanishDateToDateTimeOffset(this string date)
        {
            DateTime dt = DateTime.Parse(date);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId));
        }

        public static DateTimeOffset ZeroedHour(this long dateSeconds)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(dateSeconds);
            var utcDate = date.ToUniversalTime();
            return new DateTimeOffset(utcDate.Year, utcDate.Month, utcDate.Day, utcDate.Hour, 0, 0, TimeSpan.Zero);
        }

        public static long GetDateTimeFromMeterReadingOccurrence(string date, int timePosition, string meterReadingOccurrence)
        {
            DateTimeOffset danishDate = date.ConvertDanishDateToDateTimeOffset();

            if (meterReadingOccurrence == HourlyMeasurementsOccurrence)
            {
                return danishDate.AddHours(timePosition).ToUnixTimeSeconds();
            }
            else if (meterReadingOccurrence == QuarterlyMeasurementsOccurrence)
            {
                return danishDate.AddMinutes(timePosition * 15).ToUnixTimeSeconds();
            }

            throw new NotImplementedException($"Meter reading occurrence '{meterReadingOccurrence}' is not implemented.");
        }

        public static int GetQuantityFromMeterReading(string energyTimeSeriesMeasureUnit, string energyQuantity)
        {
            double quantityAsDouble = double.Parse(energyQuantity, NumberStyles.Number, CultureInfo.InvariantCulture);

            if (energyTimeSeriesMeasureUnit == "KWH")
            {
                return (int)Math.Round(quantityAsDouble * 1000);
            }

            throw new NotImplementedException($"Measure unit '{energyTimeSeriesMeasureUnit}' not implemented!");
        }

        public static EnergyQuantityValueQuality GetQuantityQualityFromMeterReading(string quantityQuality)
        {

            if (quantityQuality == "E01")
            {
                return EnergyQuantityValueQuality.Measured;
            }
            else if (quantityQuality == "D01")
            {
                return EnergyQuantityValueQuality.Calculated;
            }
            else if (quantityQuality == "56")
            {
                return EnergyQuantityValueQuality.Estimated;
            }
            else if (quantityQuality == "36")
            {
                return EnergyQuantityValueQuality.Revised;
            }

            throw new NotSupportedException($"QuantityQuality of type '{quantityQuality}' not supported!");
        }

        public static bool GetQuantityMissingFromMeterReading(string quantityMissingIndicator)
        {
            if (Boolean.TryParse(quantityMissingIndicator, out var result))
            {
                return result;
            }
            throw new NotSupportedException($"quantityMissingIndicator of value '{quantityMissingIndicator}' not supported!");
        }
    }
}
