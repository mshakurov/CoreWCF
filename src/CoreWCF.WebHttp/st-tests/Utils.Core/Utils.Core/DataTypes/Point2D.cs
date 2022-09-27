using System.Runtime.Serialization;

namespace ST.Utils.DataTypes
{
  /// <summary>
  /// Точка на двухмерной плоскости.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  [KnownType( typeof( BearingPoint ) )]
  public class Point2D
  {
    #region .Properties
    /// <summary>
    /// X-координата.
    /// </summary>
    [DataMember]
    public double X { get; set; }

    /// <summary>
    /// Y-координата.
    /// </summary>
    [DataMember]
    public double Y { get; set; }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var point = obj as Point2D;

      return point != null && X == point.X && Y == point.Y;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return X.GetHashCode() ^ Y.GetHashCode();
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format( "({0}, {1})", X, Y );
    }
    #endregion
  }

  /// <summary>
  /// Точка на двухмерной плоскости cо значениями скорости и направления.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  public class BearingPoint : Point2D
  {
    #region .Properties
    /// <summary>
    /// Скорость.
    /// </summary>
    [DataMember]
    public double Speed { get; set; }

    /// <summary>
    /// Направление.
    /// </summary>
    [DataMember]
    public short Direction { get; set; }
    #endregion
  }
}
