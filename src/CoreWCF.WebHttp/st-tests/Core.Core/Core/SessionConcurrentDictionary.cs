using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Diagnostics;

namespace ST.Core
{
  /// <summary>
  /// Представляет ConcurrentDictionary для работы с сессиями.
  /// </summary>
  /// <typeparam name="TKey">Ключь.</typeparam>
  /// <typeparam name="TValue">Значение.</typeparam>
  [Serializable]
  [DebuggerDisplay( "Count = {Count}" )]
  public class SessionConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    where TKey : struct
    where TValue : Session
  {
    #region .Properties
    public IAuthModule AuthModule { get; set; }
    #endregion

    #region .Ctor
    public SessionConcurrentDictionary( IAuthModule authModule )
      : base()
    {
      AuthModule = authModule;
    }
    #endregion

    #region ClearSessions
    public void ClearSessions()
    {
      if( AuthModule != null )
        AuthModule.ClearDBSessions();

      base.Clear();
    }
    #endregion

    #region GetOrAddSessions
    public TValue GetOrAddSessions( TKey key, TValue value )
    {
      if( AuthModule != null )
        AuthModule.AddDBSession( value.Id, value.Login, value.Culture.Name, value.MessageTypes.ToArray(), value.CreatedIP );

      return base.GetOrAdd( key, value );
    }
    #endregion

    #region RemoveSession
    public bool RemoveSession( TKey key )
    {
      if( AuthModule != null )
        AuthModule.RemoveDBSessions( ulong.Parse( key.ToString() ) );

      TValue value;

      return base.TryRemove( key, out value );
    }
    #endregion
  }
}
