using System;
using System.Diagnostics;
using System.Globalization;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для выполнения преобразований.
  /// </summary>
  public static class Converter
  {
    #region .Static Fields
    private static readonly char[] _hexValues = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
    #endregion

    #region ByteToHex
    /// <summary>
    /// Возвращает шестандцатиричный символ, соответствующий указанному десятичному числу.
    /// </summary>
    /// <param name="value">Десятичное число.</param>
    /// <returns>Шестандцатиричный символ.</returns>
    public static char ByteToHex( int value )
    {
      return _hexValues[value % 16];
    }
    #endregion

    #region FromBinary
    /// <summary>
    /// Возвращает строку, содержащую символьное представление массива байт.
    /// </summary>
    /// <param name="bytes">Массив байт.</param>
    /// <param name="addPrefix">Признак того, что в начало строки необходимо добавить префикс '0x'.</param>
    /// <returns>Символьное представление массива байт.</returns>
    [DebuggerStepThrough]
    public static string FromBinary( byte[] bytes, bool addPrefix = true )
    {
      string result = null;

      if( bytes != null )
      {
        var chars = new char[bytes.Length * 2 + (addPrefix ? 2 : 0)];

        if( addPrefix )
        {
          chars[0] = '0';
          chars[1] = 'x';
        }

        for( int i = 0, j = addPrefix ? 2 : 0; i < bytes.Length; i++, j += 2 )
        {
          chars[j] = ByteToHex( (bytes[i] & 0xF0) >> 4 );
          chars[j + 1] = ByteToHex( (bytes[i] & 0x0F) );
        }

        result = new string( chars );
      }

      return result;
    }
    #endregion

    #region FromString
    /// <summary>
    /// Конвертирует строковое значение в объект значимого типа, строку или байт-массив.
    /// </summary>
    /// <typeparam name="T">Тип, в который необходимо конвертировать.</typeparam>
    /// <param name="value">Строковое значение.</param>
    /// <returns>Объект типа T.</returns>
    [DebuggerStepThrough]
    public static T FromString<T>( string value )
    {
      return (T) FromString( value, typeof( T ) );
    }

    /// <summary>
    /// Конвертирует строковое значение в объект значимого типа, строку или байт-массив.
    /// </summary>
    /// <param name="value">Строковое значение.</param>
    /// <param name="type">Тип, в который необходимо конвертировать.</param>
    /// <returns>Объект типа type.</returns>
    [DebuggerStepThrough]
    public static object FromString( string value, Type type )
    {
      return type == typeof( string ) ? value :
             string.IsNullOrWhiteSpace( value ) ? (type.IsValueType ? type.CreateFast() : null) :
             type == typeof( bool ) ? Convert.ToBoolean( int.Parse( value ) ) :
             type == typeof( Enum ) ? Enum.Parse( type, value, true ) :
             type == typeof( byte[] ) ? ToBinary( value ) :
             type == typeof( TimeSpan ) ? TimeSpan.Parse( value ) :
             type.IsValueType ? Convert.ChangeType( value, type, CultureInfo.InvariantCulture ) :
             null;
    }
    #endregion

    #region HexToByte
    /// <summary>
    /// Возвращает десятичное число, соответствующее указанному шестандцатиричному символу.
    /// </summary>
    /// <param name="value">Шестандцатиричный символ.</param>
    /// <returns>Десятичное число.</returns>
    public static byte HexToByte( char value )
    {
      return (byte) (value <= '9' && value >= '0' ? value - '0' :
                     value >= 'a' && value <= 'f' ? (value - 'a') + 10 :
                     value >= 'A' && value <= 'F' ? (value - 'A') + 10 :
                     0xFF);
    }
    #endregion

    #region ToBinary
    /// <summary>
    /// Возвращает массив байт преобразованный из символьного представления.
    /// </summary>
    /// <param name="value">Символьное представление массива байт.</param>
    /// <returns>Массив байт.</returns>
    [DebuggerStepThrough]
    public static byte[] ToBinary( string value )
    {
      value = value.GetEmptyOrTrimmed();

      var skip = value.Length > 1 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X') ? 2 : 0;

      var count = ((value.Length - skip) + 1) / 2;

      var bytes = new byte[count];

      for( int i = 0, j = skip; i < count; i++, j += 2 )
        bytes[i] = (byte) ((HexToByte( value[j] ) << 4) | HexToByte( value[j + 1] ));

      return bytes;
    }
    #endregion

    #region ToString
    /// <summary>
    /// Конвертирует объект значимого типа, строку или байт-массив в строковое представление.
    /// </summary>
    /// <param name="value">Объект.</param>
    /// <returns>Строковое представление объекта.</returns>
    [DebuggerStepThrough]
    public static string ToString( object value )
    {
      var t = value != null ? value.GetType() : null;

      Func<DateTime, string> getDate = dt => dt.Date == DateTime.MinValue.Date ? dt.ToString( "HH:mm:ss.FFF" ) : dt.ToString( "yyyy-MM-ddTHH:mm:ss.FFF" );

      return t == null ? null :
             t == typeof( string ) ? value as string :
             t == typeof( bool ) ? Convert.ToInt32( value ).ToString() :
             t == typeof( byte[] ) ? FromBinary( value as byte[] ) :
             t == typeof( DateTime ) ? getDate( (DateTime) value ) :
             t == typeof( TimeSpan ) ? ((TimeSpan) value).ToString() :
             t.IsValueType ? Convert.ChangeType( value, typeof( string ), CultureInfo.InvariantCulture ) as string :
             null;
    }
    #endregion
  }
}
