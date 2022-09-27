using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using CoreWCF;
using CoreWCF.Channels;

using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Контекст сервера.
  /// </summary>
  public static class ServerContext
  {
    #region .Static Fields
    internal static bool IsActive;

    [ThreadStatic]
    [ThreadStaticContext]
    private static Session _session;

    private static readonly ConcurrentDictionary<string, PermissionDescriptor> _registeredPermissions = new ConcurrentDictionary<string, PermissionDescriptor>( StringComparer.OrdinalIgnoreCase );
    #endregion

    #region .Properties
    /// <summary>
    /// Текущая сессия.
    /// </summary>
    public static Session Session
    {
      get { return _session ?? global::ST.Core.Session.Empty; }
      internal set { _session = value; }
    }

    /// <summary>
    /// IP-адрес клиента, обращающегося к серверу.
    /// </summary>
    internal static string ClientIP
    {
      get { return OperationContext.Current == null ? "<Unknown>" : OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name].IfIs( (RemoteEndpointMessageProperty re) => re.Address ); }
    }
    #endregion

    #region ClearPermissions
    internal static void ClearPermissions()
    {
      _registeredPermissions.Clear();
    }
    #endregion

    #region GetFullPermissionName
    /// <summary>
    /// Возвращает полное название зарегистрированного разрешения доступа.
    /// </summary>
    /// <param name="code">Код разрешения доступа.</param>
    /// <returns>Полное название разрешения доступа.</returns>
    public static string GetFullPermissionName( string code )
    {
      var codes = code.Split( new [] { Permission.PERMISSION_INHERITANCE_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries );

      var names = new string[codes.Length];

      for( var i = 0; i < codes.Length; i++ )
        names[i] = GetPermission( string.Join( Permission.PERMISSION_INHERITANCE_SEPARATOR, codes, 0, i + 1 ) ).Name;

      return string.Join( Permission.PERMISSION_INHERITANCE_SEPARATOR, names );
    }
    #endregion

    #region GetPermission
    /// <summary>
    /// Возвращает зарегистрированное разрешение доступа.
    /// </summary>
    /// <param name="code">Код разрешения доступа.</param>
    /// <returns>Разрешение доступа.</returns>
    public static PermissionDescriptor GetPermission( string code )
    {
      var p = _registeredPermissions.GetValue( code );

      if( p == null )
        p = new PermissionDescriptor( code, code );

      return p;
    }
    #endregion

    #region GetPermissions
    /// <summary>
    /// Возвращает список зарегистрированных разрешений доступа.
    /// </summary>
    /// <returns>Список разрешений доступа.</returns>
    public static List<PermissionDescriptor> GetPermissions()
    {
      return _registeredPermissions.Values.ToList();
    }
    #endregion

    #region RegisterPermission
    /// <summary>
    /// Регистриует разрешение доступа.
    /// </summary>
    /// <param name="pd">Описатель разрешения доступа.</param>
    /// <returns>True - разрешение доступа успешно зарегистрировано, False - разрешение доступа с указанным кодом уже зарегистрировано.</returns>
    internal static bool RegisterPermission( [NotNull] PermissionDescriptor pd )
    {
      return _registeredPermissions.TryAdd( pd.Code, pd );
    }
    #endregion

    #region RegisterPermissions
    /// <summary>
    /// Регистриует разрешения доступа.
    /// </summary>
    /// <param name="list">Список описателей разрешения доступа.</param>
    [MethodImplAttribute( MethodImplOptions.NoInlining )]
    [CallsAllowedFrom( "ST.BusinessEntity.Server", "ST.BusinessEntity.Shell.Cache", "ST.Core" )]
    public static void RegisterPermissions( IEnumerable<PermissionDescriptor> list )
    {
      if( list != null )
        list.Where( pd => pd != null ).ForEach( pd => RegisterPermission( pd ) );
    }
    #endregion

    #region UnregisterPermission
    /// <summary>
    /// Разрегистрирует разрешение доступа.
    /// </summary>
    /// <param name="list"></param>
    [MethodImplAttribute( MethodImplOptions.NoInlining )]
    [CallsAllowedFrom( "ST.BusinessEntity.Server", "ST.BusinessEntity.Shell.Cache", "ST.Core" )]
    public static void UnregisterPermissions( IEnumerable<PermissionDescriptor> list )
    {
      if( list != null )
        list.ForEach( pd => _registeredPermissions.TryRemove( pd.Code, out pd ) );
    }
    #endregion
  }
}
