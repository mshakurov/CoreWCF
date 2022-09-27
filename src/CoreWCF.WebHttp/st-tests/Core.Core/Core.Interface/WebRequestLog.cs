using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ST.Utils;

namespace ST.Core
{

  /// <summary>
  /// Класс, позволяющий получаеть оригинальный текст вызова акции вэб-сервиса 
  /// 1) Однажды, нампример при инициализации модуля, реализующего акции веб-сервиса, регистрируем полное имя (адрес) своей акции или списка акции в WebRequestLog.Register(...)
  /// 2) В совем методе, реализующем акцию, получаем текст вызова: WebRequestLog.Get()
  /// </summary>
  public static class WebRequestLog
  {
    #region .private static
    private static ConcurrentDictionary<string, long> dict = new ConcurrentDictionary<string, long>();
    private static ConcurrentDictionary<int, string> log = new ConcurrentDictionary<int, string>();
    #endregion

    #region public methods
    /// <summary>
    /// Регистрация полного имени (адреса) акции вэб-сервиса
    /// </summary>
    /// <param name="action">Полное имя акции вэб-сервиса. Пример полного имени акции: http://www.space-team.com/RampIntegration/IRampIntegration/SetTask "</param>
    #region Register
    public static void Register( string action ) 
    {
      dict.AddOrUpdate( action, 0, ( key, count ) => count );
    }
    #endregion

    /// <summary>
    /// Регистрация списка акций вэб-сервиса
    /// </summary>
    /// <param name="actions">Список полных имен (адресов) акций вэб-сервиса</param>
    #region Register
    public static void Register( string[] actions )
    {
      Array.ForEach( actions, act => WebRequestLog.Register( act ) );
    } 
    #endregion

    /// <summary>
    /// Снятие с регистрации акции вэб-сервиса
    /// </summary>
    /// <param name="action">Полное имя акции (адреса) вэб-сервиса</param>
    #region Unregister
    public static void Unregister( string action )
    {
      long count;
      dict.TryRemove( action, out count );
    } 
    #endregion

    /// <summary>
    /// Снятие с регистрации списка акций вэб-сервиса
    /// </summary>
    /// <param name="actions">Список полных имен (адресов) акций вэб-сервиса</param>
    #region Unregister
    public static void Unregister( string[] actions )
    {
      long count;
      Array.ForEach( actions, act => dict.TryRemove( act, out count ) );
    } 
    #endregion

    /// <summary>
    /// Получение оригинального текста вызова акции вэб-сервиса
    /// </summary>
    /// <returns>Полный, оригинальный текст акции вэб-сервиса</returns>
    #region Get
    public static string Get()
    {
      string request = null;
      log.TryRemove( Thread.CurrentThread.ManagedThreadId, out request );
      return request;
    } 
    #endregion
    #endregion

    #region internal methods
    internal static bool IsRegistered( string action )
    {
      return action != null && dict.ContainsKey( action );
    }

    internal static void Log( string action, Func<string> getRequest )
    {
      if( action == null || !dict.ContainsKey( action ) )
        return;

      dict.TryUpdate( action, ( key, exCount ) => exCount + 1 );

      var request = getRequest();
      log.AddOrUpdate( Thread.CurrentThread.ManagedThreadId, request, ( key, exRequest ) => request );
    }
    #endregion

  }
}
