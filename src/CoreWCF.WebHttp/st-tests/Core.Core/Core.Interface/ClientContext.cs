namespace ST.Core
{
  /// <summary>
  /// Контекст клиента.
  /// </summary>
  public static class ClientContext
  {
    #region .Static Fields
    internal static bool IsActive;

    private static PermissionList _permissions;
    #endregion

    #region .Properties
    /// <summary>
    /// Разрешения доступа.
    /// </summary>
    public static PermissionList Permissions
    {
      get { return _permissions ?? global::ST.Core.PermissionList.Empty; }
      internal set { _permissions = value; }
    }
    #endregion

    /// <summary>
    /// Идентификатор последней сессии сессии.
    /// </summary>
    public static ulong? SessionId { get; internal set; }

  }
}
