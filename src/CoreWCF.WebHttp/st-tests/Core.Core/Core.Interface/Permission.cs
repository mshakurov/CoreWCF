using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ST.Utils;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ST.Core
{
  /// <summary>
  ///  Абстрактный класс, описывающий разрешение доступа.
  ///  Классы, являющиеся конкретными разрешениями доступа должны быть унаследованы от данного атрибута и помечены атрибутом SerializableAttribute.
  ///  Идентификатор ресурса, содержащий локализованное название разрешения доступа, извлекается из атрибута DisplayNameAttribute.
  ///  Идентификатор ресурса, содержащий локализованное описание разрешения доступа, извлекается из атрибута DescriptionAttribute.
  ///  Использовать DisplayNameAttributeLocalized и DescriptionAttributeLocalized невозможно, т.к. класс Permission является аспектом.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [Serializable]
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
  [MulticastAttributeUsage(MulticastTargets.Method, Inheritance = MulticastInheritance.None)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  public abstract class Permission : Aspect, IAspectProvider
  {
    #region .Constants
    /// <summary>
    /// Разделитель кодов разрешений доступа при наследовании.
    /// </summary>
    public const string PERMISSION_INHERITANCE_SEPARATOR = "|";
    #endregion

    #region .Static Fields
    private static readonly Dictionary<MethodBase, PermissionChecker> _permissionCheckers = new Dictionary<MethodBase, PermissionChecker>();
    #endregion

    #region GetCode
    /// <summary>
    /// Возвращает код разрешения доступа.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    /// <returns>Код разрешения доступа.</returns>
    public static string GetCode<T>()
      where T : Permission
    {
      return GetCode(typeof(T));
    }

    /// <summary>
    /// Возвращает код разрешения доступа.
    /// </summary>
    /// <param name="type">Тип разрешения доступа.</param>
    /// <returns>Код разрешения доступа.</returns>
    public static string GetCode( Type type )
    {
      var sb = new StringBuilder();

      while (type != typeof(Permission))
      {
        if (!IsValidPermissionType(type))
          return null;

        if (sb.Length > 0)
          sb.Insert(0, PERMISSION_INHERITANCE_SEPARATOR);

        sb.Insert(0, type.GetUniqueHash().ToString());

        type = type.BaseType;
      }

      return sb.ToString();
    }
    #endregion

    #region GetDescription
    /// <summary>
    /// Возвращает описание разрешения доступа.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    /// <returns>Описание разрешения доступа.</returns>
    public static string GetDescription<T>()
      where T : Permission
    {
      return GetDescription(typeof(T));
    }

    /// <summary>
    /// Возвращает описание разрешения доступа.
    /// </summary>
    /// <param name="type">Тип разрешения доступа.</param>
    /// <returns>Описание разрешения доступа.</returns>
    public static string GetDescription( Type type )
    {
      if (!IsValidPermissionType(type))
        return null;

      var descr = type.GetDescription(false, string.Empty);

      return descr == string.Empty ? string.Empty : (SR.GetString(descr) ?? descr);
    }
    #endregion

    #region GetFullName
    /// <summary>
    /// Возвращает полное название разрешения доступа.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    /// <returns>Полное название разрешения доступа.</returns>
    public static string GetFullName<T>()
      where T : Permission
    {
      return GetFullName(typeof(T));
    }

    /// <summary>
    /// Возвращает полное название разрешения доступа.
    /// </summary>
    /// <param name="type">Тип разрешения доступа.</param>
    /// <returns>Полное название разрешения доступа.</returns>
    public static string GetFullName( Type type )
    {
      var sb = new StringBuilder();

      while (type != typeof(Permission))
      {
        if (sb.Length > 0)
          sb.Insert(0, PERMISSION_INHERITANCE_SEPARATOR);

        sb.Insert(0, GetName(type));

        type = type.BaseType;
      }

      return sb.ToString();
    }
    #endregion

    #region GetName
    /// <summary>
    /// Возвращает название разрешения доступа.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    /// <returns>Название разрешения доступа.</returns>
    public static string GetName<T>()
      where T : Permission
    {
      return GetName(typeof(T));
    }

    /// <summary>
    /// Возвращает название разрешения доступа.
    /// </summary>
    /// <param name="type">Тип разрешения доступа.</param>
    /// <returns>Название разрешения доступа.</returns>
    public static string GetName( Type type )
    {
      if (!IsValidPermissionType(type))
        return null;

      var name = type.GetDisplayName(false, string.Empty);

      return name == string.Empty ? type.FullName : (SR.GetString(name) ?? name);
    }
    #endregion

    #region IsValidPermissionType
    private static bool IsValidPermissionType( Type type )
    {
      // Используется вызов стандартного метода IsDefined из-за того, что метод IsValidPermissionType вызывается косвенно из класса PermissionChecker.
      return type != null && typeof(Permission).IsAssignableFrom(type) && type.IsDefined(typeof(SerializableAttribute), false);
    }
    #endregion

    #region IAspectProvider.ProvideAspects
    IEnumerable<AspectInstance> IAspectProvider.ProvideAspects( object targetElement )
    {
      var methodBase = targetElement as MethodBase;

      PermissionChecker checker;

      if (!_permissionCheckers.TryGetValue(methodBase, out checker))
      {
        checker = new PermissionChecker(methodBase);

        _permissionCheckers.Add(methodBase, checker);
      }

      if (checker.Add(this))
        yield return new AspectInstance(methodBase, checker, checker.GetAspectConfiguration(methodBase));
    }
    #endregion

    /// <summary>
    /// Класс для внутреннего использования (сделан public из-за требования PostSharp).
    /// </summary>
    // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
    [Serializable]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [MulticastAttributeUsage(MulticastTargets.Method, Inheritance = MulticastInheritance.None)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Caching)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Modification)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Validation)]
    [ProvideAspectRole(AspectRoles.Security)]
    public sealed class PermissionChecker : OnMethodBoundaryAspect
    {
      #region .Fields
      private readonly List<string> _permissions = new List<string>();

      [NonSerialized]
      private readonly int _permissionsCount;

      [NonSerialized]
      private int _permissionNumber;
      #endregion

      #region .Ctor
      internal PermissionChecker( MethodBase methodBase )
      {
        _permissionsCount = Attribute.GetCustomAttributes(methodBase, typeof(Permission), true).Length;
      }
      #endregion

      #region Add
      internal bool Add( Permission permission )
      {
        _permissions.Add(Permission.GetCode(permission.GetType()));

        _permissionNumber++;

        return _permissionNumber == _permissionsCount;
      }
      #endregion

      #region OnEntry
      /// <summary>
      /// См. базовый класс.
      /// </summary>
      /// <param name="args">См. базовый класс.</param>
      public override void OnEntry( MethodExecutionArgs args )
      {
        try
        {
          if (ClientContext.IsActive)
            foreach (var p in _permissions)
              ClientContext.Permissions.Check(p, null);
          else
            if( ServerContext.IsActive && ServerContext.Session != Session.Empty )
              foreach( var p in _permissions )
                ServerContext.Session.Permissions.Check( p, null );
        }
        catch( AccessDeniedException ex )
        {
            var session = CoreManager.GetCurrentSession();

            var msg = string.Format("{0}{1}Method: {2}{3}Login: {4}{5}SessionId: {6}", ex.Message, Environment.NewLine, args.Method.Name, Environment.NewLine, session.Login, Environment.NewLine, session.Id);

            CoreManager.WriteToLog(msg, 1, EventLogEntryType.Warning);

          throw;
          }

        base.OnEntry(args);
      }
      #endregion
    }
  }

  public static class CoreManager
  {
    #region .Static Fields
    internal static Action<string, ushort, EventLogEntryType> WriteToLog;
    internal static Func<Session> GetCurrentSession;
    #endregion

    #region SetParameters
    /// <summary>
    /// Только для внутреннего использования.
    /// </summary>
    //[CallsAllowedFrom( "ST.Reporting.Server" )]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetParameters( Action<string, ushort, EventLogEntryType> writeToLog, Func<Session> getCurrentSession )
    {
      WriteToLog = writeToLog;
      GetCurrentSession = getCurrentSession;
    }
    #endregion
  }
}
