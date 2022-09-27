using System;

namespace ST.Core
{
  public interface ISimpleUser
  {
    #region .Properties
    /// <summary>
    /// Идентификатор организации.
    /// </summary>
    int? OrganizationId { get; set; }

    /// <summary>
    /// Идентификатор группы организациий.
    /// </summary>
    int? OrgGroupId { get; set; }

    /// <summary>
    /// Культура пользователя.
    /// </summary>
    string Culture { get; set; }

    /// <summary>
    /// Срок действия до
    /// </summary>
    DateTime? ExpireDate { get; set; }
    #endregion
  }

    public interface ILogonUser : ISimpleUser
    {
      #region .Properties
      /// <summary>
      /// Количество сессий на группу организаций.
      /// </summary>
      int? SessionCount { get; set; }

      /// <summary>
      /// Логин.
      /// </summary>
      string Login { get; set; }

      /// <summary>
      /// Идентификатор ноды.
      /// </summary>
     int? NodeId { get; set; }

     /// <summary>
     /// Адрес ноды.
     /// </summary>
     string NodeUrl { get; set; }

     /// <summary>
     /// Идентификатор ведущего пользователя.
     /// </summary>
     int? ParentUserId { get; set; }

     /// <summary>
     /// Признак использования аутентификации Windows.
     /// </summary>
     bool IsWindowsUser { get; set; }
      #endregion
    }
}
