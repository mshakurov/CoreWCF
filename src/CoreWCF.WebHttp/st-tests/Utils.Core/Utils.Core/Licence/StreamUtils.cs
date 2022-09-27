using System;
using System.IO;
using System.Text;

namespace ST.Utils.Licence
{
  public static class StreamUtils
  {
    #region .Static Fields
    public static readonly UTF8Encoding UTF8Encoding = new UTF8Encoding();
    #endregion

    #region ReadGuidReverse
    /// <summary>
    /// Статический метод расширения для чтения 
    /// значения GUID из двоичного потока в реверсивном порядке
    /// </summary>
    /// <param name="stream">поток, из которого следует считать значение</param>
    /// <returns>значение, которое считано из потока</returns>
    public static Guid ReadGuidReverse( this Stream stream )
    {
      var inputBytes = new byte[16];

      stream.Read( inputBytes, 0, inputBytes.Length );

      var a = ( (int)inputBytes[7] << 24 ) | ( (int)inputBytes[6] << 16 ) | ( (int)inputBytes[5] << 8 ) | inputBytes[4];

      var b = (short)( ( (int)inputBytes[3] << 8 ) | inputBytes[2] );

      var c = (short)( ( (int)inputBytes[1] << 8 ) | inputBytes[0] );

      var data = new byte[8];

      Array.Copy( inputBytes, 8, data, 0, data.Length );

      Array.Reverse( data );

      return new Guid( a, b, c, data );
    }
    #endregion

    #region ReadInt
    /// <summary>
    /// Статический метод расширения для чтения 
    /// целочисленного значения из двоичного потока
    /// </summary>
    /// <param name="stream">поток, из которого следует считать значение</param>
    /// <returns>значение, которое считано из потока</returns>
    public static unsafe int ReadInt( this Stream stream )
    {
      var buffer = new byte[sizeof( int )];

      stream.Read( buffer, 0, buffer.Length );

      fixed( byte* p = &buffer[0] )
      {
        return *( (int*)p );
      }
    }
    #endregion

    #region ReadLong
    /// <summary>
    /// Статический метод расширения для чтения 
    /// длинного целочисленного значения из двоичного потока
    /// </summary>
    /// <param name="stream">поток, из которого следует считать значение</param>
    /// <returns>значение, которое считано из потока</returns>
    public static unsafe long ReadLong( this Stream stream )
    {
      var buffer = new byte[sizeof( long )];

      stream.Read( buffer, 0, buffer.Length );

      fixed( byte* p = &buffer[0] )
      {
        return *( (long*)p );
      }
    }
    #endregion

    #region ReadStringUTF8
    /// <summary>
    /// Статический метод расширения для чтения 
    /// значения строки в кодировке UTF8 из двоичного потока
    /// </summary>
    /// <param name="stream">поток, из которого следует считать значение</param>
    /// <param name="stringLength">длина считываемой строки в байтах</param>
    /// <returns>значение, которое считано из потока</returns>
    public static string ReadStringUTF8( this Stream stream, int stringLength )
    {
      var buffer = new byte[stringLength];

      stream.Read( buffer, 0, buffer.Length ); // сама строка

      return UTF8Encoding.GetString( buffer );
    }
    #endregion
  }
}
