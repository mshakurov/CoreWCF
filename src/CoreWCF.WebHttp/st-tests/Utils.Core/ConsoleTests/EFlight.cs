using ST.Utils;
using System;
using System.Linq;

namespace ST.Ramp.Server.Objects
{


  /// <summary>
  /// Справочники RMS
  /// </summary>
  [Serializable]
  public partial class RMSObject
  {
    #region .Ctor
    public RMSObject()
    {
    }
    #endregion

    #region .POKO Propeties

    /// <summary>
    /// Ключ
    /// </summary>
    
    public long EntityId { get; set; }

    /// <summary>
    /// Дата создания (первой синхронизации или тестового создания)
    /// </summary>
    
    public DateTime Created { get; set; }

    /// <summary>
    /// Дата последней синхронизации с внешним источником (прием и отправка в/из Домодедово)
    /// </summary>
    
    public DateTime? LastSync { get; set; }

    /// <summary>
    /// Помечен на удаление
    /// </summary>
    
    public bool DeleteMark { get; set; }

    /// <summary>
    /// Дата последнего изменения. LastSync изменяется только при изменении из интеграции. LastUpdate изменяется так же при ручном изменении.
    /// </summary>
    /*

                  LastSync изменяется только при изменении из интеграции. LastUpdate изменяется так же при ручном изменении.
            
    */
    
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// Идентификатор объекта во внешнем источнике RMS
    /// </summary>
    
    public long oid { get; set; }

    /// <summary>
    /// Тип ID: 1-Task,2-Shift,3-Flight,4-Concat,5-Tow,6-DeviceAssignment,13-ShiftAssignement
    /// </summary>
    
    public short oid_type { get; set; }

    /// <summary>
    /// Признак исключения объекта из периодической чистки по устареванию объектов
    /// </summary>
    
    public bool NoCleanOld { get; set; }
    #endregion

    public override string ToString()
    {
      return string.Format("id:{0},type:{1},oid:{2},oid_type:{3}", this.EntityId, this.GetType().Name, this.oid, this.oid_type);
    }
  }

  #region EFlight
  /// <summary>
  /// EFlight
  /// </summary>
  public partial class EFlight : RMSObject
  {
    //oid_type must be 3;
    /// <summary>
    /// Тип объекта EFlight - должен быть 3
    /// </summary>

    #region .ctatic Ctor
    static EFlight()
    {
    }
    #endregion

    #region .Ctor
    public EFlight()
    {
    }
    #endregion

    #region .Propeties

    /// <summary>
    /// Уникальный идентификатор рейса
    /// </summary>
    
    public string Dbid { get; set; }

    /// <summary>
    /// Планируемое время рейса
    /// </summary>
    
    public DateTime? ScheduledTime { get; set; }

    /// <summary>
    /// Ожидаемое время рейса
    /// </summary>
    
    public DateTime? EstimatedTime { get; set; }

    /// <summary>
    /// Фактическое время рейса
    /// </summary>
    
    public DateTime? ActualTime { get; set; }

    /// <summary>
    /// Время Прихода/Ухода со стоянки (установка колодок)
    /// </summary>
    
    public DateTime? BlockTime { get; set; }

    /// <summary>
    /// Время отмены рейса
    /// </summary>
    
    public DateTime? CancellationTime { get; set; }

    /// <summary>
    /// Тип ВС. ссылка на AAircraftSubtype.RFSubtyp
    /// </summary>
    
    public string Atyp { get; set; }

    /// <summary>
    /// EntityId подтипа ВС - AAircraftType
    /// </summary>
    
    public int? AAircraftTypeId { get; set; }

    /// <summary>
    /// Авиакомпания. ссылка на AODB_AIRLINES.DME_CODE
    /// </summary>
    
    public string Airline { get; set; }

    /// <summary>
    /// trip number
    /// </summary>
    
    public string Trip { get; set; }

    /// <summary>
    /// Flight addition (suffix)
    /// </summary>
    
    public string Addition { get; set; }

    /// <summary>
    /// Subnumber of flight (Repeat count)
    /// </summary>
    
    public string Subnumber { get; set; }

    /// <summary>
    /// Позывной рейса
    /// </summary>
    
    public string Callsign { get; set; }

    /// <summary>
    /// Flight Function
    /// </summary>
    
    public string FlightFunction { get; set; }

    /// <summary>
    /// Рейс №
    /// </summary>
    
    public string FlightNumber { get; set; }

    /// <summary>
    /// Индикатор - прилет = 1, вылет = 0
    /// </summary>
    
    public string ADIndicator { get; set; }

    /// <summary>
    /// Регистрационный номер ВС
    /// </summary>
    
    public string Registration { get; set; }

    /// <summary>
    /// ID стык.рейса
    /// </summary>
    
    public string rotation { get; set; }

    /// <summary>
    /// Стоянка ВС, очищенная до первого подчеркивания, включая его самого. Оригинал в Stand_orig. (стоянка может быть изменена, что отражено в объекте Tow) (Stand и DSP подвергаются очистке до первого подчеркивания, включая его самого)
    /// </summary>
    
    public string Stand { get; set; }

    /// <summary>
    /// EntityId Стоянки ВС - EStandId
    /// </summary>
    
    public int? EStandId { get; set; }

    /// <summary>
    /// Фактическая сирянка ВС, очищенная до первого подчеркивания, включая его самого. Оригинал в DSP_orig. (стоянка может быть изменена, что отражено в объекте Tow) (Stand и DSP подвергаются очистке до первого подчеркивания, включая его самого)
    /// </summary>
    
    public string DSP { get; set; }

    /// <summary>
    /// EntityId Фактической стоянки по данным RMS - EStandFactId
    /// </summary>
    
    public int? EStandFactId { get; set; }

    /// <summary>
    /// Гейт. наименование  выхода на посадку
    /// </summary>
    
    public string Gate { get; set; }

    /// <summary>
    /// Полный маршрут
    /// </summary>
    
    public string Routing { get; set; }

    /// <summary>
    /// Предыдущая или следующая стоянка
    /// </summary>
    
    public string PrevNextStation { get; set; }

    /// <summary>
    /// Первая или последняя стоянка
    /// </summary>
    
    public string FirstLastStation { get; set; }

    /// <summary>
    /// Тип воздушного судна 2
    /// </summary>
    
    public string AircraftType { get; set; }

    /// <summary>
    /// Нужен буксировщик или нет
    /// </summary>
    
    public string PushbackFlag { get; set; }

    /// <summary>
    /// Время начала задачи охраны ВС
    /// </summary>
    
    public DateTime? sequrity_start { get; set; }

    /// <summary>
    /// Время окончания задачи охраны ВС. . .
    /// </summary>
    
    public DateTime? sequrity_end { get; set; }

    /// <summary>
    /// Количество пассажиров 1-го класса
    /// </summary>
    
    public long? Passengers_1Class { get; set; }

    /// <summary>
    /// Количество пассажиров бизнес класса
    /// </summary>
    
    public long? Passengers_BusinessClass { get; set; }

    /// <summary>
    /// Количество пассажиров эконом.класса
    /// </summary>
    
    public long? Passengers_EconomyClass { get; set; }

    /// <summary>
    /// Код FST - Flight.faa - Handling Type 1 IATA flight service type faa; Код FST
    /// </summary>
    
    public string FSTCode { get; set; }

    /// <summary>
    /// Стыковочный рейс - Dbid стыковочного рейса
    /// </summary>
    
    public long? ConcatFlightId { get; set; }

    /// <summary>
    /// Время постановки на стоянку. Вычисляется перед сохранением в кэш/базу
    /// </summary>
    
    public DateTime? StandOutTime { get; set; }

    /// <summary>
    /// Время ухода со стоянки. Вычисляется перед сохранением в кэш/базу
    /// </summary>
    
    public DateTime? StandInTime { get; set; }

    /// <summary>
    /// Фактическая сирянка ВС (копия DSP), в том виде, как пришла из RMS, если атрибут DSP был очищен (Stand и DSP подвергаются очистке до первого подчеркивания, включая его самого)
    /// </summary>
    
    public string DSP_orig { get; set; }

    /// <summary>
    /// Стоянка ВС (копия Stand), в том виде, как пришла из RMS, если атрибут Stand был очищен. (Stand и DSP подвергаются очистки до первого подчеркивания, включая его самого)
    /// </summary>
    
    public string Stand_orig { get; set; }

    /// <summary>
    /// 2й Позывной рейса
    /// </summary>
    
    public string Callsign2 { get; set; }

    /// <summary>
    /// 3й Позывной рейса
    /// </summary>
    
    public string Callsign3 { get; set; }

    /// <summary>
    /// Статус рейса
    /// </summary>
    
    public string FlightState { get; set; }
    #endregion
  }
  #endregion



}
