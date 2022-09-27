using System.Runtime.Serialization;

namespace ST.Utils.DataTypes
{
  /// <summary>
  /// Интервал времени.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  public class TimeRange
  {
    #region .Properties
    /// <summary>
    /// Время начала интервала (включительно).
    /// </summary>
    [DataMember]
    public DateTime From { get; set; }

    /// <summary>
    /// Время конца интервала (невключительно).
    /// </summary>
    [DataMember]
    public DateTime To { get; set; }

    /// <summary>
    /// Длительность интервала.
    /// </summary>
    [IgnoreDataMember]
    public TimeSpan Duration
    {
      get { return To - From; }
    }
    #endregion

    #region .Ctor
    public TimeRange()
    {
      From = DateTime.Now;
      To = From.AddDays( 1 );
    }

    public TimeRange( bool flag )
    {
      var now = DateTime.Now;

      From = now.Date;
      To = From.AddHours( 23 ).AddMinutes( 59 ).AddSeconds( 59 ).AddMilliseconds( 999 );
    }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var range = obj as TimeRange;

      return range != null && From == range.From && To == range.To;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return From.GetHashCode() ^ To.GetHashCode();
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format( "{0:G} - {1:G}", From, To );
    }
    #endregion

    #region GetToday
    /// <summary>
    /// Интервал, соответствующий текущему дню.
    /// </summary>
    /// <param name="utc">True, если интервал необходим в UTC.</param>
    public static TimeRange GetToday( bool utc )
    {
      var now = utc ? DateTime.UtcNow : DateTime.Now;

      return new TimeRange()
      {
        From = now.Date,
        To = now.Date.AddHours( 23 ).AddMinutes( 59 ).AddSeconds( 59 ).AddMilliseconds( 999 )
      };
    }
    #endregion

    #region GetLastDay
    /// <summary>
    /// Интерваk, соответствующий "вчера".
    /// </summary>
    /// <param name="utc">True, если интервал необходим в UTC.</param>
    public static TimeRange GetLastDay( bool utc )
    {
      var now = utc ? DateTime.UtcNow : DateTime.Now;

      return new TimeRange()
      {
        From = now.Date.AddDays( -1 ),
        To = now.Date.AddMilliseconds( -1 )
      };
    }
    #endregion

    #region GetLastWeek
    /// <summary>
    /// Интервал, соответствующий прошлой неделе.
    /// </summary>
    /// <param name="utc">True, если интервал необходим в UTC.</param>
    public static TimeRange GetLastWeek( bool utc )
    {
      var now = utc ? DateTime.UtcNow : DateTime.Now;

      var sundayOfLastWeek = now.Date.AddDays( -(int) now.DayOfWeek );
      var mondayOfLastWeek = sundayOfLastWeek.AddDays(  - 6 );

      return new TimeRange()
      {
        From = mondayOfLastWeek,
        To = sundayOfLastWeek.AddHours( 23 ).AddMinutes( 59 ).AddSeconds( 59 ).AddMilliseconds( 999 )
      };
    }
    #endregion

    #region GetLastMonth
    /// <summary>
    /// Интервал, соответствующий прошлому месяцу.
    /// </summary>
    /// <param name="utc">True, если интервал необходим в UTC.</param>
    public static TimeRange GetLastMonth( bool utc )
    {
      var now = utc ? DateTime.UtcNow : DateTime.Now;

      var prevMonthLastDate = now.Date.AddDays( -now.Date.Day );
      var prevMonthFirstDate = prevMonthLastDate.AddDays( 1 - prevMonthLastDate.Day );

      return new TimeRange()
      {
        From = prevMonthFirstDate,
        To = prevMonthLastDate.AddHours( 23 ).AddMinutes( 59 ).AddSeconds( 59 ).AddMilliseconds( 999 )
      };
    }
    #endregion
  }
}
