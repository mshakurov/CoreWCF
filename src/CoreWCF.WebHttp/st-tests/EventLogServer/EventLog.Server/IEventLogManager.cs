using System.Linq;
using System.Runtime.CompilerServices;
using ST.Utils.Attributes;
using ST.Utils;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Управление событиями. 
  /// </summary>
  public sealed partial class EventLog : IEventLogManager
  {
    #region IEventLogManager
    [Permissions.Management]
    EventTypeUser IEventLogManager.GetEventTypeUser( int typeId, int userId )
    {
      return DB.RS.Single<EventTypeUser>( "[Access].[Get]", typeId, userId );
    }

    EventTypeUser IEventLogManager.GetEventTypeCurrentUser( int typeId )
    {
      return DB.RS.Single<EventTypeUser>( "[Access].[Get]", typeId, 0 /*ISecurityManager.GetCurrentUserId()*/ );
    }

    [Permissions.Management]
    EventTypeUser[] IEventLogManager.GetEventTypeUserList()
    {
      return DB.RS.List<EventTypeUser>( "[Access].[Get]" ).ToArray();
    }

    [Permissions.Management]
    EventTypeUser[] IEventLogManager.GetListByType( int typeId )
    {
      return DB.RS.List<EventTypeUser>( "[Access].[Get]", typeId ).ToArray();
    }

    [Permissions.Management]
    EventTypeUser[] IEventLogManager.GetListByUser( int userId )
    {
      return DB.RS.List<EventTypeUser>( "[Access].[GetByUser]", userId, null ).ToArray();
    }

    [Permissions.Management]
    EventTypeDescriptor[] IEventLogManager.GetEventTypeDescriptorList()
    {
      return EventTypes.Values.ToArray();
    }

    [Permissions.Management]
    void IEventLogManager.Remove( int typeId, int userId )
    {
      DB.Execute( "[Access].[Remove]", typeId, userId );

      UpdateUserEventTypeCache( userId );
    }

    [Permissions.Management]
    void IEventLogManager.Set( [NotNull] EventTypeUser data )
    {
      DB.Execute( "[Access].[Set]", data );

      UpdateUserEventTypeCache( data.UserId );
    }

    //[CallsAllowedFrom( "ST.Ramp.Automat.Server" )]
    //[MethodImpl( MethodImplOptions.NoInlining )]
    void IEventLogManager.SetInternal( [NotNull] EventTypeUser data )
    {
      DB.Execute( "[Access].[Set]", data );

      UpdateUserEventTypeCache( data.UserId );
    }

    //[CallsAllowedFrom( "ST.Ramp.Automat.Server" )]
    //[MethodImpl( MethodImplOptions.NoInlining )]
    void IEventLogManager.RemoveInternal( int typeId, int userId )
    {
      DB.Execute( "[Access].[Remove]", typeId, userId );

      UpdateUserEventTypeCache( userId );
    }

    bool IEventLogManager.IsSubscribed( int? typeId, int userId )
    {
      var types = _userEventTypes.GetValue( userId );

      if( types == null )
        return false;

      return typeId.HasValue ? types.Any( t => t.TypeId == typeId.Value ) : types.Any( t => t.AccessType != EventAccessType.View );
    }
    #endregion
  }
}
