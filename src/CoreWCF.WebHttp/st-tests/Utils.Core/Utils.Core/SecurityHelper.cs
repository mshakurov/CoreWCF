using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для работы с безопасностью.
  /// </summary>
  public static class SecurityHelper
  {
    #region GetMD5Hash
    /// <summary>
    /// Возвращает MD5-хеш для указанной строки.
    /// </summary>
    /// <param name="str">Строка.</param>
    /// <returns>MD5-хеш.</returns>
    public static string GetMD5Hash( string str )
    {
      return str == null ? null : GetMD5Hash( Encoding.Unicode.GetBytes( str ) );
    }

    /// <summary>
    /// Возвращает MD5-хеш для указанного массива байтов.
    /// </summary>
    /// <param name="bytes">Массив байтов.</param>
    /// <returns>MD5-хеш.</returns>
    public static string GetMD5Hash( byte[] bytes )
    {
      return bytes == null ? null : Converter.FromBinary( (new MD5CryptoServiceProvider()).ComputeHash( bytes ), false );
    }

    /// <summary>
    /// Возвращает MD5-хеш для указанного потока данных.
    /// </summary>
    /// <param name="stream">Поток данных.</param>
    /// <returns>MD5-хеш.</returns>
    public static string GetMD5Hash( Stream stream )
    {
      return stream == null ? null : Converter.FromBinary( (new MD5CryptoServiceProvider()).ComputeHash( stream ), false );
    }
    #endregion

    #region GetSimpleMD5
    public static string GetSimpleMD5( string input )
    { 
      using( System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create() )
      {
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes( input );
        byte[] hashBytes = md5.ComputeHash( inputBytes );

        StringBuilder sb = new StringBuilder();
        for( int i = 0; i < hashBytes.Length; i++ )
        {
          sb.Append( hashBytes[i].ToString( "X2" ) );
        }
        return sb.ToString();
      }
    } 
    #endregion

    #region GetSHA256Hash
    /// <summary>
    /// Возвращает SHA256-хеш для указанной строки.
    /// </summary>
    /// <param name="str">Строка.</param>
    /// <returns>SHA256-хеш.</returns>
    public static string GetSHA256Hash( string str )
    {
      return str == null ? null : GetSHA256Hash( Encoding.Unicode.GetBytes( str ) );
    }

    /// <summary>
    /// Возвращает SHA256-хеш для указанного массива байтов.
    /// </summary>
    /// <param name="bytes">Массив байтов.</param>
    /// <returns>SHA256-хеш.</returns>
    public static string GetSHA256Hash( byte[] bytes )
    {
      return bytes == null ? null : Converter.FromBinary( (new SHA256CryptoServiceProvider()).ComputeHash( bytes ), false );
    }

    /// <summary>
    /// Возвращает SHA256-хеш для указанного потока данных.
    /// </summary>
    /// <param name="stream">Поток данных.</param>
    /// <returns>SHA256-хеш.</returns>
    public static string GetSHA256Hash( Stream stream )
    {
      return stream == null ? null : Converter.FromBinary( (new SHA256CryptoServiceProvider()).ComputeHash( stream ), false );
    }
    #endregion
  }
}
