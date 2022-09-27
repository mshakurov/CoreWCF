using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace ST.Utils.Threading
{
  /// <summary>
  /// Класс для передачи контекста со значениями статических полей, помеченных атрибутами ThreadStaticAttribute и ThreadStaticContextAttribute, из одного потока в другой.
  /// Также, передается информация о культуре исходного потока.
  /// </summary>
  public sealed class ThreadStaticContext
  {
    #region .Static Fields
    private static readonly HashSet<FieldInfo> _fields = new HashSet<FieldInfo>();
    private static readonly object _syncLocker = new object();
    #endregion

    #region .Fields
    private readonly Dictionary<FieldInfo, object> _fieldValues = new Dictionary<FieldInfo, object>();

    private readonly CultureInfo _culture = Thread.CurrentThread.CurrentCulture;
    private readonly CultureInfo _uiCulture = Thread.CurrentThread.CurrentUICulture;
    #endregion

    #region .Ctor
    private ThreadStaticContext()
    {
      lock( _syncLocker )
        _fields.ForEachTry( fi => _fieldValues.Add( fi, fi.GetValue( null ) ) );
    }
    #endregion

    #region Capture
    /// <summary>
    /// Захватывает в контекст значения всех статических полей, помеченных атрибутами ThreadStaticAttribute и ThreadStaticContextAttribute.
    /// Для того, чтобы статическое поле попало в контекст, должен отработать статический конструктор класса, в котором объявлено поле, до вызова данного метода.
    /// </summary>
    /// <returns>Захваченный контекст.</returns>
    public static ThreadStaticContext Capture()
    {
      return new ThreadStaticContext();
    }
    #endregion

    #region Execute
    /// <summary>
    /// Выполняет действие в рамках контекста.
    /// </summary>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public void Execute( Action action )
    {
      Restore();

      action();
    }
    #endregion

    #region Register
    internal static void Register( FieldInfo fieldInfo )
    {
      lock( _syncLocker )
        _fields.Add( fieldInfo );
    }
    #endregion

    #region Restore
    /// <summary>
    /// Восстанавливает значение полей из контекста, захваченного раннее методом Capture.
    /// </summary>
    public void Restore()
    {
      Thread.CurrentThread.CurrentCulture = _culture;
      Thread.CurrentThread.CurrentUICulture = _uiCulture;

      lock( _syncLocker )
        _fieldValues.ForEachTry( f => f.Key.SetValue( null, f.Value ) );
    }
    #endregion
  }
}
