using System;
using System.Diagnostics;
using Castle.DynamicProxy;

namespace ST.Utils.Reflection
{
  /// <summary>
  /// Базовый класс-перехватчик для вызова методов удаленного объекта.
  /// </summary>
  public class SimpleInterceptor : IInterceptor
  {
    #region Intercept
    [DebuggerStepThrough]
    public virtual void Intercept( IInvocation invocation )
    {
      try
      {
        OnBeforeInvoke( invocation );

        invocation.Proceed();

        OnAfterInvoke( invocation );
      }
      catch( Exception exc )
      {
        OnCatch( invocation, exc );

//#if !DEBUG
        throw;
//#endif
      }
      finally
      {
        OnFinally( invocation );
      }
    }
    #endregion

    #region OnAfterInvoke
    /// <summary>
    /// Вызывается после вызова метода удаленного объекта.
    /// </summary>
    /// <param name="invocation">Информация о вызове метода.</param>
    [DebuggerStepThrough]
    protected virtual void OnAfterInvoke( IInvocation invocation )
    {
    }
    #endregion

    #region OnBeforeInvoke
    /// <summary>
    /// Вызывается перед вызовом метода удаленного объекта.
    /// </summary>
    /// <param name="invocation">Информация о вызове метода.</param>
    [DebuggerStepThrough]
    protected virtual void OnBeforeInvoke( IInvocation invocation )
    {
    }
    #endregion

    #region OnCatch
    /// <summary>
    /// Вызывается в блоке catch после вызова метода удаленного объекта.
    /// </summary>
    /// <param name="invocation">Информация о вызове метода.</param>
    /// <param name="exc">Исключение.</param>
    [DebuggerStepThrough]
    protected virtual void OnCatch( IInvocation invocation, Exception exc )
    {
    }
    #endregion

    #region OnFinally
    /// <summary>
    /// Вызывается в блоке finally после вызова метода удаленного объекта.
    /// </summary>
    /// <param name="invocation">Информация о вызове метода.</param>
    [DebuggerStepThrough]
    protected virtual void OnFinally( IInvocation invocation )
    {
    }
    #endregion
  }
}
