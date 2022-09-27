using System.Runtime.Serialization;

namespace ST.Utils.DataTypes
{
  /// <summary>
  /// Интервал дат, границы которого могут быть не заданы.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  public class OpenDateRange
  {
    #region .Fields
    private DateTime? _from;
    private DateTime? _to;
    #endregion

    #region .Properties
    /// <summary>
    /// Дата начала интервала (включительно).
    /// </summary>
    [DataMember]
    public DateTime? From
    {
      get { return _from; }
      set { _from = value.HasValue ? value.Value.Date : (DateTime?) null; }
    }

    /// <summary>
    /// Дата конца интервала (включительно).
    /// </summary>
    [DataMember]
    public DateTime? To
    {
      get { return _to; }
      set { _to = value.HasValue ? value.Value.Date : (DateTime?) null; }
    }

    /// <summary>
    /// Длительность интервала.
    /// </summary>
    [IgnoreDataMember]
    public TimeSpan? Duration
    {
      get { return _to.HasValue && _from.HasValue ? (_to.Value - _from.Value) + TimeSpan.FromDays( 1 ) : (TimeSpan?) null; }
    }

    public OpenTimeRange TimeRange
    {
      get { return new OpenTimeRange { From = _from, To = _to.HasValue ? _to.Value.AddDays( 1 ) : (DateTime?) null }; }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public OpenDateRange()
    {
      _to = _from = DateTime.Now.Date;
    }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var range = obj as OpenDateRange;

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
