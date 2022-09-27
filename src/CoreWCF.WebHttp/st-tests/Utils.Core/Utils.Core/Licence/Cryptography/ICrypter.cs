namespace ST.Utils.Licence
{
  public interface ICrypter
  {
    byte[] Encode( byte[] input );

    byte[] Decode( byte[] input );

    int GetAligmentSize( int dataSize );
  }
}
