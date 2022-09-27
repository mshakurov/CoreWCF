using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ST.Utils.Attributes;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Linq;

namespace ST.Utils
{
  /// <summary>
  /// Общие вспомогательные extension-методы.
  /// </summary>
  public static class Extensions
  {
    #region .Static Fields
    //[DllImport( "msvcrt.dll", CallingConvention = CallingConvention.Cdecl )]
    //static extern int memcmp( byte[] b1, byte[] b2, long count );

    private static readonly string _exceptionBlockSeparator = new string( '-', 80 );
    private static readonly string[] _newLineSeparator = new[] { Environment.NewLine };

    [ThreadStatic]
    private static StringBuilder _sb;

    private static readonly HashSet<Type> _criticalExceptions = new HashSet<Type>
    {
      typeof( OutOfMemoryException ),
      typeof( StackOverflowException ),
      typeof( AccessViolationException ),
      typeof( AppDomainUnloadedException ),
      typeof( BadImageFormatException ),
      typeof( DivideByZeroException )
    };

    private static readonly Dictionary<CultureInfo, string> _cultures = new Dictionary<CultureInfo, string>
    {
      { CultureInfo.GetCultureInfo( "ru" ), SR.GetString( RI.Culture_ru ) },
      { CultureInfo.GetCultureInfo( "ru-RU" ), SR.GetString( RI.Culture_ruRU ) },
      { CultureInfo.GetCultureInfo( "en-US" ), SR.GetString( RI.Culture_enUS ) }
    };

    private static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );

    private static readonly ConcurrentDictionary<Type, bool> _primitiveTypes = new ConcurrentDictionary<Type, bool>();
    #endregion

    #region AppendLine
    private static StringBuilder AppendLine( this StringBuilder sb, string value, int indentLevel, string indent = "    " )
    {
      foreach ( var line in value.Split( _newLineSeparator, StringSplitOptions.None ) )
      {
        for ( var i = 0; i < indentLevel; i++ )
          sb.Append( indent );

        sb.AppendLine( line );
      }

      return sb;
    }
    #endregion

    #region StringBuilder Limited
    public static bool AppendLineLimited( this StringBuilder sb, string line, int maxTotalLength )
    {
      var addingLength = ( sb.Length > 0 ? Environment.NewLine.Length : 0 ) + line.Length;
      if ( addingLength <= maxTotalLength )
      {
        if ( sb.Length > 0 )
          sb.Append( Environment.NewLine );
        sb.Append( line );
        return true;
      }
      return false;
    }

    public static bool AppendLineLimited( this StringBuilder sb, int maxTotalLength )
    {
      if ( sb.Length + Environment.NewLine.Length <= maxTotalLength )
      {
        sb.Append( Environment.NewLine );
        return true;
      }
      return false;
    }

    public static bool AppendLineLimited( this StringBuilder sb, int maxTotalLength, string format, params object[] args )
    {
      return sb.AppendLineLimited( string.Format( format, args ), maxTotalLength );
    }

    public static bool AppendLimited( this StringBuilder sb, string str, int maxTotalLength )
    {
      if ( sb.Length + str.Length <= maxTotalLength )
      {
        sb.Append( str );
        return true;
      }
      return false;
    }

    public static bool AppendLimited( this StringBuilder sb, int maxTotalLength, string format, params object[] args )
    {
      return sb.AppendLimited( string.Format( format, args ), maxTotalLength );
    }
    #endregion

    #region ByteArrayCompare
    /// <summary>
    /// Возвращает результат сравнения массивов байт.
    /// </summary>
    /// <param name="b1">Массив байт.</param>
    /// <param name="b2">Массив байт.</param>
    /// <returns>True - массивы равны, иначе False.</returns>
    //[DebuggerStepThrough]
    //public static bool ByteArrayCompare( byte[] b1, byte[] b2 )
    //{
    //  return b1.Length == b2.Length && memcmp( b1, b2, b1.Length ) == 0;
    //}
    [DebuggerStepThrough]
    public static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
    {
      return a1.SequenceEqual(a2);
    }
    #endregion

    #region GetDisplayName
    /// <summary>
    /// Возвращает локализованное название культуры.
    /// </summary>
    /// <param name="cultureInfo">Культура.</param>
    /// <returns>Локализованное название.</returns>
    public static string GetDisplayName( [NotNull] this CultureInfo cultureInfo )
    {
      return _cultures.GetValue( cultureInfo ) ?? cultureInfo.DisplayName;
    }
    #endregion

    #region GetEmptyOrTrimmed
    /// <summary>
    /// Возвращает пустую строку, если переданная строка равна null, или переданную строку с усечением начальных и конечных пробелов.
    /// </summary>
    /// <param name="name">Строка.</param>
    /// <returns>Пустая или усеченная строка.</returns>
    public static string GetEmptyOrTrimmed( this string name )
    {
      return name == null ? string.Empty : name.Trim();
    }
    #endregion

    #region GetFullMessage
    /// <summary>
    /// Возвращает полное описание исключения (описание исходного исключения + описания всех вложенных исключений).
    /// </summary>
    /// <param name="exc">Исходное исключение.</param>
    /// <param name="includeHeaders">Признак того, что в описание исключения необходимо включить в виде заголовков полные названия типов исключений.</param>
    /// <param name="includeStackTrace">Признак того, что в описание исключения необходимо включить трассировку стэка.</param>
    /// <param name="getWin32Code">Включать ли в сообщение код ошибки Win32</param>
    /// <returns>Полный текст исключения.</returns>
    public static string GetFullMessage( [NotNull] this Exception exc, bool includeHeaders = false, bool includeStackTrace = false, bool getWin32Code = false )
    {
      if ( _sb == null )
        _sb = new StringBuilder( 4096 );

      _sb.Clear();

      GetFullMessage( exc, includeHeaders, includeStackTrace, true, 0, getWin32Code );

      return _sb.ToString();
    }

    private static void GetFullMessage( Exception exc, bool includeHeaders, bool includeStackTrace, bool root, int level, bool getWin32Code )
    {
      if ( exc != null )
        exc = exc.GetRealException();

      if ( exc is AggregateException aggExc )
        aggExc.InnerExceptions.ForEach( e => GetFullMessage( e, includeHeaders, includeStackTrace, true, level + 1, getWin32Code ) );
      else
        if ( exc != null )
        {
          if ( _sb.Length > 0 )
            _sb.AppendLine();

          if ( includeHeaders )
          {
            _sb.AppendLine( exc.GetType().FullName, level );
            _sb.AppendLine( _exceptionBlockSeparator, level );
          }

          var msg = exc.Message;

          if ( getWin32Code && exc is Win32Exception excWin32 )
            _sb.AppendFormat( "Code: {0}. ", excWin32.NativeErrorCode );

          if ( msg != null )
            _sb.AppendLine( msg, level );

          GetFullMessage( exc.InnerException, includeHeaders, includeStackTrace, false, level + 1, getWin32Code );

          if ( root )
          {
            var stackTrace = includeStackTrace ? exc.StackTrace : null;

            if ( stackTrace != null )
            {
              _sb.AppendLine();
              _sb.AppendLine( _exceptionBlockSeparator, level );
              _sb.AppendLine( stackTrace, level );
              _sb.AppendLine( _exceptionBlockSeparator, level );
            }
          }
        }
    }
    #endregion

    #region GetRealException
    /// <summary>
    /// Если исходным исключением является TargetInvocationException, то метод возвращает первое вложенное исключение не являющееся TargetInvocationException.
    /// Иначе возвращается exc.
    /// </summary>
    /// <param name="exc">Исходное исключение.</param>
    /// <returns>Настоящее исключение.</returns>
    public static Exception GetRealException( [NotNull] this Exception exc )
    {
      while ( exc.GetType() == typeof( TargetInvocationException ) )
        exc = exc.InnerException;

      return exc;
    }
    #endregion

    #region GetUniqueHash
    /// <summary>
    /// Возвращает уникальный детерминированный хэш-код для строки.
    /// </summary>
    /// <param name="str">Строка.</param>
    /// <returns>Хэш-код.</returns>
    [DebuggerStepThrough]
    public static unsafe int GetUniqueHash([NotNull] this string str)
    {
      int hash = 0;

      fixed (char* s = str)
      {
        int* ptr = (int*)s;

        int i = str.Length;

        for (; i > 0; i -= 2)
          hash = hash * 101 + *ptr++;
      }

      return hash;
    }

    /// <summary>
    /// Возвращает уникальный детерминированный хэш-код для типа. Хэш-код определяется по полному названию типа.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <returns>Хэш-код.</returns>
    [DebuggerStepThrough]
    public static int GetUniqueHash([NotNull] this Type type)
    {
      return type.FullName.GetUniqueHash();
    }
    #endregion

    #region IndexRange
    /// <summary>
    /// Возвращает коллекцию элементов списка в пределах заданных индексов (включительно).
    /// </summary>
    /// <typeparam name="TSource">Тип элемента списка.</typeparam>
    /// <param name="source">Список элементов.</param>
    /// <param name="fromIndex">Начальный индекс.</param>
    /// <param name="toIndex">Конечный индекс.</param>
    /// <returns>Коллекция элементов списка.</returns>
    public static IEnumerable<TSource> IndexRange<TSource>( this IList<TSource> source, int fromIndex, int toIndex )
    {
      int currentIndex = fromIndex;

      while ( currentIndex <= toIndex )
      {
        yield return source[currentIndex];

        currentIndex++;
      }
    }
    #endregion

    #region EqualsWithKey
    [DebuggerStepThrough]
    public static bool EqualsWithKey<TSource, TKey>( this TSource first, TSource second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null )
    {
      if ( comparer == null )
        comparer = EqualityComparer<TKey>.Default;
      if ( comparer == null )
        return
          first == null && second == null
          ||
          first != null && second != null && keySelector( first ).Equals( keySelector( second ) );
      else
        return
          first == null && second == null
          ||
          first != null && second != null && comparer.Equals( keySelector( first ), keySelector( second ) );
    }
    #endregion

    #region IsEqualNullable
    [DebuggerStepThrough]
    public static bool IsEqualNullable( this object val1, object val2 )
    {
      return
        object.ReferenceEquals( val1, val2 )
        ||
        (object)val1 != null && (object)val2 != null
        &&
        (
          val1.GetType().Equals( val2.GetType() )
          &&
          (
            ( val1 is string )
            ?
            ( (string)val1 ).Equals( (string)val2, StringComparison.OrdinalIgnoreCase )
            :
            val1.Equals( val2 )
          )
        );
    }


    [DebuggerStepThrough]
    public static bool IsEqualNullable<T>( this T? val1, T? val2 )
      where T : struct
    {
      return
        val1 == null && val2 == null
        ||
        val1 != null && val2 != null
        &&
        val1.Value.Equals( val2.Value );
    }
    #endregion

    #region CollectionsEqual
    [DebuggerStepThrough]
    public static bool CollectionsEqual<T>( this ICollection<T> one, ICollection<T> other, bool strictOrder = false )
    {
      if ( object.ReferenceEquals( one, other ) )
        return true;
      if (
        ( ( object )one == null || ( object )other == null )
        ||
        one.Count != other.Count
        )
        return false;
      if ( strictOrder )
      {
        var eoter = other.GetEnumerator();
        foreach ( T iOne in one )
        {
          if ( !eoter.MoveNext() )
            return false;
          if ( !iOne.Equals( eoter.Current ) )
            return false;
        }
        if ( eoter.MoveNext() )
          return false;
        return true;
      }
      else
      {
        foreach ( T iOne in one )
          if ( !other.Contains( iOne ) )
            return false;
        return true;
      }
    }

    [DebuggerStepThrough]
    public static bool CollectionsEqual<T>( this ICollection<T> one, ICollection<T> other, Func<T, T, bool> equalityComparer, bool strictOrder )
    {
      if ( equalityComparer == null )
        return CollectionsEqual( one, other, strictOrder );

      if ( object.ReferenceEquals( one, other ) )
        return true;
      if (
        ( ( object )one == null || ( object )other == null )
        ||
        one.Count != other.Count
        )
        return false;

      if ( strictOrder )
      {
        var eoter = other.GetEnumerator();
        foreach ( T iOne in one )
        {
          if ( !eoter.MoveNext() )
            return false;
          if ( !equalityComparer( iOne, eoter.Current ) )
            return false;
        }
        if ( eoter.MoveNext() )
          return false;
        return true;
      }
      else
      {
        foreach ( T iOne in one )
        {
          bool found = false;
          foreach ( T iOther in other )
            if ( found = equalityComparer( iOne, iOther ) )
              break;
          if ( !found )
            return false;
        }
        return true;
      }
    } 
    #endregion

    #region InRange
    /// <summary>
    /// Проверяет, находится ли значение в указанном диапазоне.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="value">Значение.</param>
    /// <param name="left">Левая граница диапазона.</param>
    /// <param name="right">Правая граница диапазона.</param>
    /// <returns>True - значение находится внтури границ диапазона включительно, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool InRange<T>( this T value, T left, T right )
      where T : IComparable<T>
    {
      return value != null ? value.CompareTo( left ) >= 0 && value.CompareTo( right ) <= 0 : false;
    }

    /// <summary>
    /// Проверяет, находится ли значение в указанном диапазоне.
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <param name="left">Левая граница диапазона.</param>
    /// <param name="right">Правая граница диапазона.</param>
    /// <returns>True - значение находится внтури границ диапазона включительно, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool InRange( this IComparable value, object left, object right )
    {
      return value != null ? value.CompareTo( left ) >= 0 && value.CompareTo( right ) <= 0 : false;
    }
    #endregion

    #region IsCritical
    /// <summary>
    /// Возвращает признак того, что исключение является критическим.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <returns>True - исключение является критическим, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool IsCritical( [NotNull] this Exception exc )
    {
      return _criticalExceptions.Contains( exc.GetType() );
    }
    #endregion

    #region IsEqualCI
    /// <summary>
    /// Определяет эквивалентность строк без учета регистра.
    /// </summary>
    /// <param name="str1">Строка.</param>
    /// <param name="str2">Строка.</param>
    /// <returns>True - строки равны, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool IsEqualCI( this string str1, string str2 )
    {
      return string.Compare( str1, str2, StringComparison.OrdinalIgnoreCase ) == 0;
    }
    #endregion

    #region SafeAccess
    /// <summary>
    /// Выполняет действие с учетом безопасного доступа к объекту (double check).
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="syncObject">Объект, к которому выполняется доступ.</param>
    /// <param name="condition">Условие доступа.</param>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public static void SafeAccess<T>( [NotNull] this T syncObject, Func<bool> condition, Action action )
      where T : class
    {
      if ( condition() )
        lock ( syncObject )
          if ( condition() )
            action();
    }
    #endregion

    #region NormilizeNewLine
    public static string NormilizeNewLine( this string source )
    {
      return source.Replace( "\r\n", "\n" ).Replace( "\n", "\r\n" );
    }
    #endregion

    #region ToUnixTime
    public static long ToUnixTime( this DateTime dateTime )
    {
      return ( dateTime - UnixEpoch ).Ticks / TimeSpan.TicksPerMillisecond;
    }
    #endregion

    #region ToDateTime
    public static DateTime ToDateTime( this long unixTimeStamp )
    {
      DateTime dtDateTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc );

      dtDateTime = dtDateTime.AddMilliseconds( unixTimeStamp );

      return dtDateTime;
    }
    #endregion

    #region Truncate
    public static DateTime Truncate( this DateTime dateTime, long resolution )
    {
      return new DateTime( dateTime.Ticks - ( dateTime.Ticks % resolution ), dateTime.Kind );
    }

    public static DateTime? Truncate( this DateTime? dateTime, long resolution )
    {
      return dateTime.HasValue ? new DateTime( dateTime.Value.Ticks - ( dateTime.Value.Ticks % resolution ), dateTime.Value.Kind ) : null as DateTime?;
    }
    #endregion

    #region TrimLength
    /// <summary>
    /// Уменьшает длину строки до заданной величены, если длина строки больше этой величины.
    /// </summary>
    /// <param name="source">Исходная строка</param>
    /// <param name="length">Новая длина строки (если строка длиннее)</param>
    /// <returns>Уменьшенная строка</returns>
    public static string TrimLength( this string source, int length )
    {
      if ( source.Length > length )
        return source.Remove( length );
      return source;
    } 
    #endregion

    #region NullIfWhiteSpace
    public static string NullIfWhiteSpace( this string source, Func<string, string> update = null )
    {
      if ( string.IsNullOrWhiteSpace( source ) )
        return null;

      if ( update != null )
      {
        source = update( source );
        if ( string.IsNullOrWhiteSpace( source ) )
          return null;
      }

      return source;
    } 
    #endregion

    #region IsTypeHasNameCI
    /// <summary>
    /// Для отладчика, проверяет, имеет ли объект тип с указанным коротким названием
    /// </summary>
    /// <param name="obj">Объект</param>
    /// <param name="typeName">Краткое название типа</param>
    /// <returns>имеет ли объект тип с указанным коротким названием</returns>
    public static bool IsTypeHasNameCI( object obj, string typeName )
    {
      return obj.GetType().Name.IsEqualCI( typeName );
    }

    /// <summary>
    /// Для отладчика, проверяет, имеет ли объект тип с указанным коротким названием
    /// </summary>
    /// <param name="obj">Объект</param>
    /// <param name="typeFullName">Краткое название типа</param>
    /// <returns>имеет ли объект тип с указанным коротким названием</returns>
    public static bool IsTypeHasFullNameCI( object obj, string typeFullName )
    {
      return obj.GetType().FullName.IsEqualCI( typeFullName );
    } 
    #endregion

    #region IsSimple
    /// <summary>
    /// Проверяет, является ли тип простым, т.е.: примитивным или типом значения или строкой или decimal
    /// </summary>
    /// <param name="type">Тип</param>
    /// <returns>True - простой</returns>
    public static bool IsSimple( this Type type )
    {
      return _primitiveTypes.GetOrAdd( type, _type =>
        {
          if ( _type.IsGenericType && _type.GetGenericTypeDefinition() == typeof( Nullable<> ) )
            _type = _type.GetGenericArguments()[0];
          return _type.IsPrimitive
            || _type.IsValueType
            || _type.IsEnum
            || _type.Equals( typeof( string ) );
        } );
    } 
    #endregion

    #region SplitByLength
    public static string[] SplitByLength( this string str, int length )
    {
      if ( str.Length == 0 )
        return new string[0];
      var count = str.Length / length + ( str.Length % length > 0 ? 1 : 0 );
      var array = new string[count];
      for ( int i = 0; i < count; i++ )
        array[i] = str.Substring( i * length, Math.Min( str.Length - i * length, length ) );
      return array;
    }
    #endregion

  }
}
