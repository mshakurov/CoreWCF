using ST.Utils;

namespace ST.Telematics.Server.Entities
{
  /// <summary>
  /// Объекты телематики.
  /// </summary>
  public class TelematicsObject : MonObj
  {

    #region .Propeties
    /// <summary>
    /// Минимальное время фиксации стоянки, в минутах.
    /// </summary>
    public int? StopTimeFixacion { get; set; }

    /// <summary>
    /// Пороговая скорость стоянки, в км/час.
    /// </summary>
    public double? StopSpeedLimit { get; set; }

    /// <summary>
    /// Разрешенная скорость.
    /// </summary>
    public double? SpeedLimit { get; set; }
    #endregion
  }
}
