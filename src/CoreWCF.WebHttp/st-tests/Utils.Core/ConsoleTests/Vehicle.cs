using System;
using ST.BusinessEntity;
using ST.BusinessEntity.Server;
using ST.Telematics.Server.Entities;
using ST.Utils;

namespace ST.Monitoring.Server.Entities
{
  /// <summary>
  /// Транспортное средство.
  /// </summary>
  public class Vehicle : TelematicsObject
  {
    #region .Propeties
    /// <summary>
    /// Гос. №.
    /// </summary>
    public virtual string Plate { get; set; }

    /// <summary>
    /// Гар. №.
    /// </summary>
    public virtual string GarageNumber { get; set; }

    /// <summary>
    /// Идентификатор марки транспортного средства.
    /// </summary>
    public int? VehicleTypeId { get; set; }

    /// <summary>
    /// Идентификатор организации.
    /// </summary>
    public int? OrganizationId { get; set; }

    /// <summary>
    /// Дата выпуска.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// VIN.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// Текущий пробег.
    /// </summary>
    public double CurrentRun { get; set; }

    /// <summary>
    /// Идентификатор водителя.
    /// </summary>
    public int? DriverId { get; set; }

    /// <summary>
    /// Идентификатор маршрута.
    /// </summary>
    public int? RouteId { get; set; }

    /// <summary>
    /// Идентификатор группы организаций.
    /// </summary>
    public int? OrgGroupId { get; set; }

    /// <summary>
    /// Идентификатор внешнего статуса.
    /// </summary>
    public int? ExternalStateId { get; set; }


    /// <summary>
    /// Время привязки водителя.
    /// </summary>
    public DateTime? DriverChangeTime { get; set; }

    /// <summary>
    /// Идентификатор топливной карты.
    /// </summary>
    public string? FuelCardId { get; set; }
    #endregion
  }
}
