using System.Runtime.Serialization;

namespace ST.Utils.DataTypes
{
  /// <summary>
  /// Интервал времени, границы которого могут быть не заданы.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  public class OpenTimeRange
  {
    #region .Properties
    /// <summary>
    /// Время начала интервала (включительно).
    /// </summary>
    [DataMember]
    public DateTime? From { get; set; }

    /// <summary>
    /// Время конца интервала (невключительно).
    /// </summary>
    [DataMember]
    public DateTime? To { get; set; }

    /// <summary>
    /// Длительность интервала.
    /// </summary>
    [IgnoreDataMember]
    public TimeSpan? Duration
    {
      get { return To.HasValue && From.HasValue ? To - From : (TimeSpan?) null; }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public OpenTimeRange()
    {
      From = DateTime.Now;
      To = From.Value.AddDays( 1 );
    }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var range = obj as OpenTimeRange;

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
  }
}
