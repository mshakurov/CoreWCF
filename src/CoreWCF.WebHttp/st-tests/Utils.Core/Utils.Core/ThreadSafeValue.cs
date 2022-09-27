using System.Threading;
namespace ST.Utils
{
  #region ThreadSafeObject
  public class ThreadSafeObject<TObject>
    where TObject : class
  {
    private object _locker = new object();
    private TObject _object = null;
    public TObject Value
    {
      get
      {
        TObject result = null;
        lock( _locker )
          result = _object;
        return result;
      }
      set
      {
        lock( _locker )
          _object = value;
      }
    }

    public ThreadSafeObject()
    {
    }

    public ThreadSafeObject( TObject initial )
    {
      _object = initial;
    }

    public bool CompareExchange(TObject value, TObject comparable)
    {
      lock( _locker )
      {
        var equals = _object == comparable;
        if (equals)
          _object = value;
        return equals;
      }
    }

    public bool CompareExchangeByRef( TObject value, TObject comparable )
    {
      lock( _locker )
      {
        var equals = object.ReferenceEquals(_object, comparable);
        if( equals )
          _object = value;
        return equals;
      }
    }

  }
  #endregion

  #region ThreadSafeValue
  public class ThreadSafeValue<TObject>
    where TObject : struct
  {
    private object _locker = new object();
    private TObject _object = default( TObject );
    public TObject Value
    {
      get
      {
        lock( _locker )
          return _object;
      }
      set
      {
        lock( _locker )
          _object = value;
      }
    }

    public ThreadSafeValue( TObject initialValue )
    {
      Value = initialValue;
    }
  }
  #endregion

  public class ThreadSafeBool
  {
    private int _value;

    public bool Value
    {
      get
      {
        return _value == 1;
      }
      set
      {
        Interlocked.Exchange( ref _value, value ? 1 : 0 );
      }
    }

    public ThreadSafeBool(bool value)
    {
      Value = value;
    }

    public static implicit operator ThreadSafeBool( bool value )
    {
      return new ThreadSafeBool( value );
    }

    public static implicit operator bool( ThreadSafeBool value )
    {
      return value.Value;
    }
  }
}
