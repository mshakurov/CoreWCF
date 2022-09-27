using System;
using System.IO;

namespace ST.Utils.Licence
{
  public class CryptContext
  {
    #region .Static Fields
    private static Random _rnd = new Random();
    #endregion

    #region .Fields
    private ICrypter _crypter;
    #endregion

    #region .Ctor
    public CryptContext( ICrypter crypter )
    {
      if( crypter == null )
        throw new ArgumentNullException();

      _crypter = crypter;
    }
    #endregion

    #region Decode
    public byte[] Decode( Stream stream )
    {
      var input = GetBytesFromStream( stream, 0 );

      var newInput = new byte[input.Length];

      Array.Copy( input, newInput, newInput.Length );

      return _crypter.Decode( newInput );
    }
    #endregion

    #region Encode
    public byte[] Encode( Stream stream )
    {
      var input = GetBytesFromStream( stream, 0 );

      var aligmentSize = _crypter.GetAligmentSize( input.Length );

      var newInput = new byte[stream.Length + aligmentSize];

      Array.Copy( input, newInput, input.Length );

      if( aligmentSize != 0 )
      {
        var buffer = new byte[aligmentSize];

        _rnd.NextBytes( buffer );

        Array.Copy( buffer, 0, newInput, input.Length, buffer.Length );
      }

      return _crypter.Encode( newInput );
    }
    #endregion

    #region GetBytesFromStream
    private static byte[] GetBytesFromStream( Stream stream, int offset )
    {
      if( stream != null )
      {
        stream.Position += offset;

        var bytes = new byte[stream.Length - stream.Position];

        stream.Read( bytes, 0, bytes.Length );

        return bytes;
      }

      return null;
    }
    #endregion
  }
}
  