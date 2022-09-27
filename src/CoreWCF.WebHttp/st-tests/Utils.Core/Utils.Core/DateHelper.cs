using System;

namespace ST.Utils
{
  public static class DateHelper
  {
    public static readonly DateTime OldDateTime = new DateTime( 1900, 1, 1 );

    public static DateTime Max( DateTime dt1, DateTime dt2 )
    {
      return dt1 > dt2 ? dt1 : dt2;
    }

    public static DateTime? Max( DateTime? dt1, DateTime? dt2 )
    {
      return dt1 > dt2 ? ( dt1 ?? dt2 ) : ( dt2 ?? dt1 );
    }

    public static DateTime Max( DateTime dt1, DateTime dt2, DateTime dt3 )
    {
      return DateHelper.Max( DateHelper.Max( dt1, dt2 ), dt3 );
    }

    public static DateTime? Max( DateTime? dt1, DateTime? dt2, DateTime? dt3 )
    {
      return DateHelper.Max( DateHelper.Max( dt1, dt2 ), dt3 );
    }

    public static DateTime Min( DateTime dt1, DateTime dt2 )
    {
      return dt1 < dt2 ? dt1 : dt2;
    }

    public static DateTime? Min( DateTime? dt1, DateTime? dt2 )
    {
      return dt1 < dt2 ? ( dt1 ?? dt2 ) : ( dt2 ?? dt1 );
    }

    public static DateTime Min( DateTime dt1, DateTime dt2, DateTime dt3 )
    {
      return DateHelper.Min( DateHelper.Min( dt1, dt2 ), dt3 );
    }

    public static DateTime? Min( DateTime? dt1, DateTime? dt2, DateTime? dt3 )
    {
      return DateHelper.Min( DateHelper.Min( dt1, dt2 ), dt3 );
    }

    public static DateTime Near( DateTime dt1, DateTime dt2, DateTime dtTarget )
    {
      return ( dtTarget - dt1 ).Ticks > ( dtTarget - dt2 ).Ticks ? dt1 : dt2;
    }

    public static bool IsFirstNearest( DateTime dt1, DateTime dt2, DateTime dtTarget )
    {
      return Math.Abs( ( dtTarget - dt1 ).Seconds ) <= Math.Abs( ( dtTarget - dt2 ).Seconds );
    }

    public static bool Between( this DateTime time, DateTime timeFrom, DateTime timeTo )
    {
      return time >= timeFrom && time <= timeTo;
    }

    public static DateTime TruncateToSeconds( this DateTime value )
    {
      return new DateTime( value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind );
    }

    public static DateTime TruncateToMinutes( this DateTime value )
    {
      return new DateTime( value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind );
    }

    public static DateTime TruncateToHours( this DateTime value )
    {
      return new DateTime( value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Kind );
    }

    /// <summary>
    /// Очищает TimeSpan до Дней, Часов и Минут
    /// </summary>
    /// <param name="value">Исходное значение, которое надо очистить</param>
    /// <returns>Значение value, очищенное до Дней, Часов и Минут</returns>
    public static TimeSpan TruncateToMinutes( this TimeSpan value )
    {
      return new TimeSpan( value.Days, value.Hours, value.Minutes, 0 );
    }

    /// <summary>
    /// Очищает TimeSpan до Дней, Часов, Минут и Секунд
    /// </summary>
    /// <param name="value">Исходное значение, которое надо очистить</param>
    /// <returns>Значение value, очищенное до Дней, Часов и Минут</returns>
    public static TimeSpan TruncateToSeconds( this TimeSpan value )
    {
      return new TimeSpan( value.Days, value.Hours, value.Minutes, value.Seconds );
    }

    public static bool Intercects( DateTime timeFrom1, DateTime timeTo1, DateTime timeFrom2, DateTime timeTo2 )
    {
      if ( timeFrom1 > timeTo1 || timeFrom2 > timeTo2 )
        throw new ArgumentException();

      return !( timeFrom2 > timeTo1 || timeTo2 < timeFrom1 );
    }

    public static DateTime MinValueIfNull( this DateTime? value )
    {
      return value.HasValue ? value.Value : DateTime.MinValue;
    }
  }
}
