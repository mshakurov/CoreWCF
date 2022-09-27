using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Список разрешений доступа.
  /// </summary>
  [Serializable]
  public sealed class PermissionList : IEnumerable<string>, IEnumerable
  {
    #region .Static Fields
    /// <summary>
    /// Пустые разрешения доступа.
    /// </summary>
    public static readonly PermissionList Empty = new PermissionList();

#if DEBUG
    private static readonly bool _isPermissionCheckDisabled = EnvironmentHelper.IsCommandLineArgumentDefined( "DisablePermissionCheck" );
#endif
    #endregion

    #region .Fields
    private readonly HashSet<string> _permissions = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
    private readonly object _syncLock = new object();

    private readonly bool _readOnly;

    private string _thumbprint = string.Empty;

    private readonly StringBuilder _stringBuilder = new StringBuilder();
    #endregion

    #region .Properties
    /// <summary>
    /// Количество разрешений доступа.
    /// </summary>
    public int Count
    {
      get { return _permissions.Count; }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public PermissionList()
    {
    }

    internal PermissionList( bool readOnly )
    {
      _readOnly = readOnly;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="permissions">Список типов разрешений доступа.</param>
    /// <param name="readOnly">Если True, то набор разрешений доступа будет доступен только для чтения.</param>
    public PermissionList( [NotNull] IEnumerable<Type> permissions, bool readOnly = false ) : this( readOnly )
    {
      AddRange( permissions, false );
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="permissions">Список кодов разрешений доступа.</param>
    /// <param name="readOnly">Если True, то набор разрешений доступа будет доступен только для чтения.</param>
    public PermissionList( [NotNull] IEnumerable<string> permissions, bool readOnly = false ) : this( readOnly )
    {
      AddRange( permissions, false );
    }
    #endregion

    #region Add
    /// <summary>
    /// Добавляет разрешение доступа.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    public void Add<T>()
      where T : Permission
    {
      Add( Permission.GetCode<T>() );
    }

    /// <summary>
    /// Добавляет разрешение доступа.
    /// </summary>
    /// <param name="permission">Код разрешения доступа.</param>
    public void Add( [NotNull] string permission )
    {
      Add( permission, true );
    }

    internal void Add<T>( bool checkReadOnly )
      where T : Permission
    {
      Add( Permission.GetCode<T>(), checkReadOnly );
    }

    internal void Add( [NotNull] string permission, bool checkReadOnly )
    {
      if( checkReadOnly && _readOnly )
        return;

      lock( _syncLock )
      {
        _permissions.Add( permission );

        MakeThumbprint();
      }
    }
    #endregion

    #region AddRange
    /// <summary>
    /// Добавляет список разрешений доступа.
    /// </summary>
    /// <param name="permissions">Список типов разрешений доступа.</param>
    public void AddRange( [NotNull] IEnumerable<Type> permissions )
    {
      AddRange( permissions.Where( t => typeof( Permission ).IsAssignableFrom( t ) ).Select( t => Permission.GetCode( t ) ) );
    }

    /// <summary>
    /// Добавляет список разрешений доступа.
    /// </summary>
    /// <param name="permissions">Список кодов разрешений доступа.</param>
    public void AddRange( [NotNull] IEnumerable<string> permissions )
    {
      AddRange( permissions, true );
    }

    internal void AddRange( [NotNull] IEnumerable<Type> permissions, bool checkReadOnly )
    {
      AddRange( permissions.Where( t => typeof( Permission ).IsAssignableFrom( t ) ).Select( t => Permission.GetCode( t ) ), checkReadOnly );
    }

    internal void AddRange( [NotNull] IEnumerable<string> permissions, bool checkReadOnly )
    {
      if( checkReadOnly && _readOnly )
        return;

      lock( _syncLock )
      {
        permissions.ForEach( p => _permissions.Add( p ) );

        MakeThumbprint();
      }
    }
    #endregion

    #region RemoveRange
    public void RemoveRange( [NotNull] IEnumerable<string> permissions )
    {
      RemoveRange( permissions, true );
    }

    internal void RemoveRange( [NotNull] IEnumerable<string> permissions, bool checkReadOnly )
    {
      if( checkReadOnly && _readOnly )
        return;

      lock( _syncLock )
      {
        permissions.ForEach( p => _permissions.Remove( p ) );

        MakeThumbprint();
      }
    }
    #endregion

    #region Check
    /// <summary>
    /// Проверяет, содержит ли список разрешений доступа указанное разрешение.
    /// В случае отсутствия указанного разрешения выбрасывается исключение AccessDeniedException.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    /// <param name="failDescriptionGetter">Метод, возвращающий описание ошибки для исключения AccessDeniedException. Если null, то будет использовано описание по умолчанию.</param>
    public void Check<T>( Func<string> failDescriptionGetter = null )
      where T : Permission
    {
      Check( Permission.GetCode<T>(), failDescriptionGetter );
    }

    /// <summary>
    /// Проверяет, содержит ли список разрешений доступа указанное разрешение.
    /// В случае отсутствия указанного разрешения выбрасывается исключение AccessDeniedException.
    /// </summary>
    /// <param name="permissionType">Тип разрешения доступа. Должен быть унаследован он Permission.</param>
    /// <param name="failDescriptionGetter">Метод, возвращающий описание ошибки для исключения AccessDeniedException. Если null, то будет использовано описание по умолчанию.</param>
    public void Check( [NotNull] Type permissionType, Func<string> failDescriptionGetter = null )
    {
      Check( typeof( Permission ).IsAssignableFrom( permissionType ) ? Permission.GetCode( permissionType ) : permissionType.FullName, failDescriptionGetter );
    }

    /// <summary>
    /// Проверяет, содержит ли список разрешений доступа указанное разрешение.
    /// В случае отсутствия указанного разрешения выбрасывается исключение AccessDeniedException.
    /// </summary>
    /// <param name="permission">Код разрешения доступа.</param>
    /// <param name="failDescriptionGetter">Метод, возвращающий описание ошибки для исключения AccessDeniedException. Если null, то будет использовано описание по умолчанию.</param>
    public void Check( [NotNullNotEmpty] string permission, Func<string> failDescriptionGetter = null )
    {
#if DEBUG
      if (!_isPermissionCheckDisabled)
#endif
        if (!Contains(permission))
          throw new AccessDeniedException( permission, failDescriptionGetter == null ? null : failDescriptionGetter() );
    }
    #endregion

    #region Clear
    /// <summary>
    /// Удаляет все разрешения доступа.
    /// </summary>
    public void Clear()
    {
      Clear( true );
    }

    internal void Clear( bool checkReadOnly )
    {
      if( checkReadOnly && _readOnly )
        return;

      lock( _syncLock )
      {
        _permissions.Clear();

        _thumbprint = string.Empty;
      }
    }
    #endregion

    #region Contains
    /// <summary>
    /// Проверяет, содержит ли список разрешений доступа указанное разрешение.
    /// </summary>
    /// <typeparam name="T">Тип разрешения доступа.</typeparam>
    /// <returns>True - список содержит проверяемое разрешение, иначе - False.</returns>
    public bool Contains<T>()
      where T : Permission
    {
      return Contains( Permission.GetCode<T>() );
    }

    /// <summary>
    /// Проверяет, содержит ли список разрешений доступа указанное разрешение.
    /// </summary>
    /// <param name="permissionType">Тип разрешения доступа. Должен быть унаследован он Permission.</param>
    /// <returns>True - список содержит проверяемое разрешение, иначе - False.</returns>
    public bool Contains( [NotNull] Type permissionType )
    {
      return typeof( Permission ).IsAssignableFrom( permissionType ) && Contains( Permission.GetCode( permissionType ) );
    }

    /// <summary>
    /// Проверяет, содержит ли список разрешений доступа указанное разрешение.
    /// </summary>
    /// <param name="permission">Код разрешения доступа.</param>
    /// <returns>True - список содержит проверяемое разрешение, иначе - False.</returns>
    public bool Contains( [NotNullNotEmpty] string permission )
    {
#if DEBUG
      if( _isPermissionCheckDisabled )
        return true;
#endif

      return _permissions.Contains( permission );
    }
    #endregion

    #region Equals
    /// <summary>
    /// См. базовый класс.
    /// </summary>
    /// <param name="obj">См. базовый класс.</param>
    /// <returns>См. базовый класс.</returns>
    public override bool Equals( object obj )
    {
      var p = obj as PermissionList;

      return p != null && _thumbprint == p._thumbprint;
    }
    #endregion

    #region GetEnumerator
    /// <summary>
    /// Возвращает перечислитель списка разрешений доступа.
    /// </summary>
    /// <returns>Перечислитель.</returns>
    public IEnumerator<string> GetEnumerator()
    {
      lock( _syncLock )
        return _permissions.ToArray().AsEnumerable().GetEnumerator();
    }
    #endregion

    #region GetHashCode
    /// <summary>
    /// См. базовый класс.
    /// </summary>
    /// <returns>См. базовый класс.</returns>
    public override int GetHashCode()
    {
      return _thumbprint.GetHashCode();
    }
    #endregion

    #region MakeThumbprint
    private void MakeThumbprint()
    {
      try
      {
        if( _permissions.Count > 0 )
        {
          _permissions.OrderBy( p => p ).ForEach( p => _stringBuilder.Append( p ) );

          _thumbprint = SecurityHelper.GetMD5Hash( _stringBuilder.ToString().ToUpper() );
        }
      }
      finally
      {
        _stringBuilder.Clear();
      }
    }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строковое представление списка разрешений доступа.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return _thumbprint;
    }
    #endregion

    #region IEnumerable.GetEnumerator
    IEnumerator IEnumerable.GetEnumerator()
    {
      lock( _syncLock )
        return _permissions.ToArray().GetEnumerator();
    }
    #endregion
  }
}
