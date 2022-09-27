using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.Core
{
  /// <summary>
  /// Описание сессии, сохраненной в БД.
  /// </summary>
  [Serializable]
  public sealed class DBSession
  {
    #region .Properties
    /// <summary>
    /// Идентификатор сессии.
    /// </summary>
    public ulong SessionId { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Логин.
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// IP-адрес создания сессии.
    /// </summary>
    public string CreatedIP { get; set; }

    /// <summary>
    /// Идентификатор группы.
    /// </summary>
    public int? OrgGroupId { get; set; }

    /// <summary>
    /// Разрешения.
    /// </summary>
    public PermissionList Permissions { get; set; }

    /// <summary>
    /// Культура.
    /// </summary>
    public string Culture { get; set; }

    /// <summary>
    /// Подписки.
    /// </summary>
    public string[] MessageTypes { get; set; }

    /// <summary>
    /// Результат аутентификации пользователя.
    /// </summary>
    public object AuthenticationResult { get; set; }
    #endregion
  }
}
