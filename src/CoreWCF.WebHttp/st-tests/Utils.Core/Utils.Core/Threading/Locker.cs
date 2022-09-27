using System;
using System.Threading;

namespace ST.Utils.Threading
{
  /// <summary>
  /// Предоставляет механизм для синхронизации доступа к ресурсам.
  /// </summary>
  public class Locker : IDisposable
  {
    #region .Fields
    private readonly object _syncLock = new object();

    private int _lockOwner = -1;
    private int _lockCount;
    #endregion

    #region .Properties
    /// <summary>
    /// Идентификатор потока, владеющего эксклюзивным доступом.
    /// Если эксклюзивным доступом не владеет ни один поток, то возвращается -1.
    /// </summary>
    public int Owner
    {
      get { return _lockOwner; }
    }

    /// <summary>
    /// Количество запросов эксклюзивного доступа, полученных текущим потоком.
    /// </summary>
    public int Reentrants
    {
      get { return _lockCount; }
    }
    #endregion

    #region .Events
    /// <summary>
    /// Вывывается непосредственно после получения потоком эксклюзивного доступа.
    /// </summary>
    public event Action Acquired;

    /// <summary>
    /// Вызывается непосредственно перед освобождением потоком эксклюзивного доступа.
    /// </summary>
    public event Action Released;
    #endregion

    #region Acquire
    /// <summary>
    /// Получает эксклюзивный доступ для текущего потока. Каждый вызов данного метода должен сопровождаться вызовом метода Release.
    /// В действительности, получение эксклюзивного доступа происходит при первом вызове данного метода вызывающим потоком.
    /// Может использоваться в конструкции using: using( locker.Acquire() ) { ... }.
    /// </summary>
    /// <returns>Объект для освобождения эксклюзивного доступа.</returns>
    public IDisposable Acquire()
    {
      Monitor.Enter( _syncLock );

      _lockCount++;

      if( _lockCount == 1 )
      {
        _lockOwner = Thread.CurrentThread.ManagedThreadId;

        OnAcquired();
      }

      return this;
    }
    #endregion

    #region Release
    /// <summary>
    /// Освобождает эксклюзивный доступ для текущего потока.
    /// В действительности, освобождение эксклюзивного доступа происходит при последнем вызове данного метода вызывающим потоком (соответствующего первому вызову метода Acquire).
    /// </summary>
    public void Release()
    {
      if( _lockOwner == Thread.CurrentThread.ManagedThreadId && _lockCount > 0 )
      {
        _lockCount--;

        if( _lockCount == 0 )
        {
          _lockOwner = -1;

          OnReleased();
        }

        Monitor.Exit( _syncLock );
      }
    }
    #endregion

    #region OnAcquired
    /// <summary>
    /// Вывывается непосредственно после получения потоком эксклюзивного доступа.
    /// </summary>
    public virtual void OnAcquired()
    {
      if( Acquired != null )
        Acquired();
    }
    #endregion

    #region OnReleased
    /// <summary>
    /// Вызывается непосредственно перед освобождением потоком эксклюзивного доступа.
    /// </summary>
    public virtual void OnReleased()
    {
      if( Released != null )
        Released();
    }
    #endregion

    #region IDisposable
    void IDisposable.Dispose()
    {
      Release();
    }
    #endregion
  }
}
