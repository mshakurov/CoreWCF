using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ST.Core;
using ST.Utils;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Доступность пользователя к типу события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  [KnownType( "GetSubtypes" )]
  public class EventTypeUser
  {
    #region .Properties
    /// <summary>
    /// Идентификатор типа события.
    /// </summary>
    [DataMember]
    public int TypeId { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    [DataMember]
    public int UserId { get; set; }

    /// <summary>
    /// Идентификатор типа реакции на событие.
    /// </summary>
    [DataMember]
    public virtual EventAccessType AccessType { get; set; }

    /// <summary>
    /// Признак всплывающего окна на событие.
    /// </summary>
    [DataMember]
    public virtual bool NotificationRequired { get; set; }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return TypeId.GetHashCode() ^ UserId.GetHashCode();
    }
    #endregion 

    #region Equals
    public override bool Equals( object obj )
    {
      if( ReferenceEquals( this, obj ) )
        return true;

      var user = obj as EventTypeUser;

      return user != null && user.TypeId == TypeId && user.UserId == UserId;
    }
    #endregion 

    #region GetSubtypes
    /// <summary>
    /// Возвращает список всех найденных в сборках домена типов событий.
    /// </summary>
    /// <returns>Список типов событий.</returns>
    public static IEnumerable<Type> GetSubtypes()
    {
      return AssemblyHelper.GetSubtypes( false, new[] { typeof( EventTypeUser ) }, typeof( PlatformAssemblyAttribute ) );
    }
    #endregion
  }  
}
