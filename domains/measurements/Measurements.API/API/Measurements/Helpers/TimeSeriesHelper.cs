using System;
using System.Globalization;
using Measurements.V1;

namespace API.Measurements.Helpers
{
    public static class TimeSeriesHelper
    {
        private const string TimeZoneId = "Romance Standard Time";

        private static DateTimeOffset ConvertDanishDateToDateTimeOffset(this string date)
        {
            DateTime dt = DateTime.Parse(date);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId));
        }

        public static DateTimeOffset ConvertToDanishTimezone(this DateTimeOffset date)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(date, TimeZoneId);
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

            if (meterReadingOccurrence == "PT1H")
            {
                return danishDate.AddHours(timePosition).ToUnixTimeSeconds();
            }
            else if (meterReadingOccurrence == "PT15M")
            {
                return danishDate.AddMinutes(timePosition * 15).ToUnixTimeSeconds();
            }

            throw new NotImplementedException($"Meter reading occurance '{meterReadingOccurrence}' is not implemented.");
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

        public static EnergyQuantityValueQuality GetQuantityQualityFromMeterReading(string QuantityQuality)
        {

            if (QuantityQuality == "E01")
            {
                return EnergyQuantityValueQuality.Measured;
            }
            else if (QuantityQuality == "D01")
            {
                return EnergyQuantityValueQuality.Calculated;
            }
            else if (QuantityQuality == "56")
            {
                return EnergyQuantityValueQuality.Estimated;
            }
            else if (QuantityQuality == "36")
            {
                return EnergyQuantityValueQuality.Revised;
            }

            throw new NotSupportedException($"QuantityQuality of type '{QuantityQuality}' not supported!");
        }
    }
}
