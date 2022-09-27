using ST.Utils;

using System.Collections.Concurrent;
using System.Globalization;

namespace ST.Core
{
  /// <summary>
  /// Сессия.
  /// </summary>
  [Serializable]
  public class Session
  {
    #region .Static Fields
    private static int _sessionId = 1;

    /// <summary>
    /// Пустая сессия.
    /// </summary>
    public static readonly Session Empty = new Session( 0, null, null, null, PermissionList.Empty, null, null, null, 0 );
    #endregion

    #region .Fields
    private readonly ulong _id;
    private readonly string _login;
    private readonly DateTime _createdTime;
    private readonly string _createdIP;
    private DateTime _lastAccessedTime;
    private string _lastAccessedIP;
    private DateTime _expirationTime;
    private long _incomingTraffic;
    private long _outgoingTraffic;
    private DateTime _lastUpdateTrafficTime;
    private int _saveTrafficTime;

    private readonly PermissionList _permissions;

    private readonly CultureInfo _culture;

    private readonly int? _orgGroupId;

    private readonly string _productName;

    internal readonly IAuthModule AuthModule;

    internal readonly ConcurrentDictionary<BaseModule, ConcurrentDictionary<Type, object>> Data = new ConcurrentDictionary<BaseModule, ConcurrentDictionary<Type, object>>();

    internal readonly ConcurrentQueue<CommunicationMessage.Wrapper> Messages = new ConcurrentQueue<CommunicationMessage.Wrapper>();

    internal readonly HashSet<string> MessageTypes = new HashSet<string>();
    #endregion

    #region .Properties
    /// <summary>
    /// Идентификатор сессии.
    /// </summary>
    public ulong Id
    {
      get { return _id; }
    }

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public string Login
    {
      get { return _login; }
    }

    /// <summary>
    /// Время создания сессии.
    /// </summary>
    public DateTime CreatedTime
    {
      get { return _createdTime; }
    }

    /// <summary>
    /// IP-адрес создания сессии.
    /// </summary>
    public string CreatedIP
    {
      get { return _createdIP; }
    }

    /// <summary>
    /// Время последнего доступа к сессии.
    /// </summary>
    public DateTime LastAccessedTime
    {
      get { return _lastAccessedTime; }
    }

    /// <summary>
    /// IP-адрес последнего доступа к сессии.
    /// </summary>
    public string LastAccessedIP
    {
      get { return _lastAccessedIP; }
    }

    /// <summary>
    /// Время истечения сессии.
    /// </summary>
    public DateTime ExpirationTime
    {
      get { return _expirationTime; }
    }

    /// <summary>
    /// Разрешения доступа.
    /// </summary>
    public PermissionList Permissions
    {
      get { return _permissions; }
    }

    public CultureInfo Culture
    {
      get { return _culture ?? Thread.CurrentThread.CurrentUICulture; }
    }

    /// <summary>
    /// Идентификатор группы организаций.
    /// </summary>
    public int? OrgGroupId
    {
      get { return _orgGroupId; }
    }

    /// <summary>
    /// Название продукта.
    /// </summary>
    public string ProductName
    {
      get { return _productName; }
    }

    /// <summary>
    /// Входящий трафик.
    /// </summary>
    public long IncomingTraffic
    {
      get { return _incomingTraffic; }
    }

    /// <summary>
    /// Исходящий трафик.
    /// </summary>
    public long OutgoingTraffic
    {
      get { return _outgoingTraffic; }
    }

    /// <summary>
    /// Дата, когда последний раз пользователю посылалсь сообщение о приближении срока окончания учетной записи
    /// </summary>
    public DateTime? AccountExpirationWarningSentDate { get; set; }
    #endregion

    #region .Ctor
    internal Session( ulong id, IAuthModule authModule, string login, string createdIP, PermissionList permissions, CultureInfo culture, int? orgGroupId, string productName, int saveTrafficTime )
    {
      AuthModule = authModule;

      _id = id;
      _login = login;

      if( authModule == null )
      {
        _lastAccessedTime = DateTime.UtcNow;
        _lastAccessedIP = "localhost";
        _expirationTime = DateTime.MaxValue;
      }
      else
        Refresh();

      _createdTime = _lastAccessedTime;
      _createdIP = createdIP ?? _lastAccessedIP;
      _permissions = new PermissionList( permissions, true );
      _culture = culture;
      _orgGroupId = orgGroupId;
      _productName = productName;
      AccountExpirationWarningSentDate = DateTime.UtcNow;
      _lastUpdateTrafficTime = DateTime.UtcNow;
      _saveTrafficTime = saveTrafficTime;
    }
    #endregion

    #region AddRangeSafe
    public void AddRangeSafe( string[] messageTypes )
    {
      MessageTypes.AddRangeSafe( messageTypes );

      if( AuthModule != null )
        AuthModule.SetMessageTypes( _id, messageTypes );
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
      var session = obj as Session;

      return session != null && _id == session._id;
    }
    #endregion

    #region GetHashCode
    /// <summary>
    /// См. базовый класс.
    /// </summary>
    /// <returns>См. базовый класс.</returns>
    public override int GetHashCode()
    {
      return _id.GetHashCode();
    }
    #endregion

    #region GetNextSessionId
    public static ulong GetNextSessionId()
    {
      var rand = new Random();

      var count = rand.Next( 3, 13 );

      for( var i = 0; i < count; i++ )
        Interlocked.Increment( ref _sessionId );

      return (((ulong) Interlocked.Increment( ref _sessionId )) << 32) | (((uint) rand.Next( 1, int.MaxValue )));
    }
    #endregion

    #region Refresh
    internal void Refresh()
    {
      _lastAccessedIP = ServerContext.ClientIP;
      _lastAccessedTime = DateTime.UtcNow;
      _expirationTime = _lastAccessedTime + TimeSpan.FromMilliseconds( Interface.Constants.SESSION_LIFE_TIME );
    }
    #endregion

    #region Refresh
    internal void UpdateTraffic( long? incomming, long? outgoing )
    {
      if( _saveTrafficTime == 0 )
        return;

      _incomingTraffic += incomming.GetValueOrDefault( 0 );
      _outgoingTraffic += outgoing.GetValueOrDefault( 0 );

      if( AuthModule != null && ((!incomming.HasValue && !outgoing.HasValue) || _lastUpdateTrafficTime.AddMinutes( _saveTrafficTime ) < DateTime.UtcNow) )
      {
        _lastUpdateTrafficTime = DateTime.UtcNow;

        AuthModule.OnUserTrafficUpdate();

        _incomingTraffic = 0;
        _outgoingTraffic = 0;
      }
    }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строковое представление сессии.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return this == Empty ? "Empty session" : SR.GetString( Interface.RI.SessionId, Id ) + Environment.NewLine +
                                               SR.GetString( Interface.RI.SessionLogin, Login ) + Environment.NewLine +
                                               SR.GetString( Interface.RI.SessionCreated, CreatedTime, CreatedIP ) + Environment.NewLine +
                                               SR.GetString( Interface.RI.SessionAccessed, LastAccessedTime, LastAccessedIP );
    }
    #endregion
  }
}
