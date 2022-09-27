using System;
using System.Runtime.Serialization;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Базовый описатель объекта данных, относящегося к событиям.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public abstract class EventBaseDescriptor
  {
    #region .Properties
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [DataMember]
    public int Id { get; set; }

    /// <summary>
    /// Название.
    /// </summary>
    [DataMember]
    public string Name { get; set; }

    /// <summary>
    /// Категория.
    /// </summary>
    [DataMember]
    public string Category { get; set; }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var descr = obj as EventBaseDescriptor;

      return descr != null && descr.Id == Id;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return Id;
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return Name;
    }
    #endregion
  }

  /// <summary>
  /// Описатель типа события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public class EventTypeDescriptor : EventBaseDescriptor
  {
  }

  /// <summary>
  /// Описатель категории события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public class CategoryDescriptor : EventBaseDescriptor
  { 
  }

  /// <summary>
  /// Описатель источника события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public class SourceDescriptor : EventBaseDescriptor
  {
  }
}
