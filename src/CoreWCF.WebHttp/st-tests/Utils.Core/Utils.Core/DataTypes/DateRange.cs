using System.Runtime.Serialization;

namespace ST.Utils.DataTypes
{
  /// <summary>
  /// Интервал дат.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  public class DateRange
  {
    #region .Fields
    private DateTime _from;
    private DateTime _to;
    #endregion

    #region .Properties
    /// <summary>
    /// Дата начала интервала (включительно).
    /// </summary>
    [DataMember]
    public DateTime From
    {
      get { return _from; }
      set { _from = value.Date; }
    }

    /// <summary>
    /// Дата конца интервала (включительно).
    /// </summary>
    [DataMember]
    public DateTime To
    {
      get { return _to; }
      set { _to = value.Date; }
    }

    /// <summary>
    /// Длительность интервала.
    /// </summary>
    [IgnoreDataMember]
    public TimeSpan Duration
    {
      get { return (_to - _from) + TimeSpan.FromDays( 1 ); }
    }

    public TimeRange TimeRange
    {
      get { return new TimeRange { From = _from, To = _to.AddDays( 1 ) }; }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public DateRange()
    {
      _to = _from = DateTime.Now.Date;
    }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var range = obj as DateRange;

      return range != null && _from == range._from && _to == range._to;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return _from.GetHashCode() ^ _to.GetHashCode();
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format( "{0:d} - {1:d}", _from, _to );
    }
    #endregion
  }
}
